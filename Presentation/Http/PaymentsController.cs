using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.BuildingBlocks.Responses;
using Platform.Contracts.Payments;
using Platform.Payment.API.Application.Features.Payments.Commands.ProcessWebhook;
using Platform.Payment.API.Infrastructure.Configurations;
using Microsoft.Extensions.Options;
using System.Net;
using System.Text.Json;

namespace Platform.Payment.API.Presentation.Http;

[Route("api/payments")]
[ApiController]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly SandboxPaymentOptions _sandboxOptions;
    private readonly IWebHostEnvironment _environment;

    public PaymentsController(
        ISender sender,
        IOptions<SandboxPaymentOptions> sandboxOptions,
        IWebHostEnvironment environment)
    {
        _sender = sender;
        _sandboxOptions = sandboxOptions.Value;
        _environment = environment;
    }

    [HttpPost("webhooks/{provider}")]
    [AllowAnonymous]
    public async Task<IActionResult> HandleWebhook(string provider, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body);
        var rawBody = await reader.ReadToEndAsync(cancellationToken);

        var command = new ProcessPaymentWebhookCommand(provider, rawBody);
        var result = await _sender.Send(command, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("sandbox/checkout/{referenceCode:long}")]
    public IActionResult SandboxCheckout(long referenceCode, [FromQuery] string? paymentLinkId)
    {
        if (!IsSandboxEnabled())
            return NotFound();

        var encodedPaymentLinkId = WebUtility.HtmlEncode(paymentLinkId ?? string.Empty);
        var successUrl = $"/api/payments/sandbox/complete/{referenceCode}?paymentLinkId={Uri.EscapeDataString(paymentLinkId ?? string.Empty)}&result=success";
        var cancelUrl = $"/api/payments/sandbox/complete/{referenceCode}?paymentLinkId={Uri.EscapeDataString(paymentLinkId ?? string.Empty)}&result=cancel";

        var html = $$"""
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <title>Sandbox Payment</title>
    <style>
        :root {
            color-scheme: light;
            --bg: #f7f1e8;
            --card: #fffaf2;
            --text: #1f2937;
            --muted: #6b7280;
            --line: #e5d7bf;
            --success: #166534;
            --danger: #991b1b;
            --accent: #b45309;
        }
        * { box-sizing: border-box; }
        body {
            margin: 0;
            min-height: 100vh;
            display: grid;
            place-items: center;
            background:
                radial-gradient(circle at top, rgba(245, 158, 11, 0.15), transparent 35%),
                linear-gradient(180deg, var(--bg), #f3eadc);
            color: var(--text);
            font-family: "Segoe UI", Arial, sans-serif;
            padding: 24px;
        }
        .card {
            width: min(100%, 520px);
            background: var(--card);
            border: 1px solid var(--line);
            border-radius: 20px;
            padding: 28px;
            box-shadow: 0 18px 40px rgba(31, 41, 55, 0.08);
        }
        .eyebrow {
            display: inline-block;
            font-size: 12px;
            font-weight: 700;
            letter-spacing: 0.12em;
            text-transform: uppercase;
            color: var(--accent);
            margin-bottom: 12px;
        }
        h1 {
            margin: 0 0 12px;
            font-size: 28px;
            line-height: 1.1;
        }
        p {
            margin: 0 0 18px;
            color: var(--muted);
            line-height: 1.6;
        }
        .meta {
            margin: 0 0 24px;
            padding: 16px;
            border-radius: 14px;
            border: 1px solid var(--line);
            background: rgba(255, 255, 255, 0.7);
        }
        .meta strong {
            display: block;
            margin-bottom: 6px;
            font-size: 14px;
        }
        .actions {
            display: grid;
            gap: 12px;
        }
        .button {
            display: inline-flex;
            justify-content: center;
            align-items: center;
            min-height: 48px;
            padding: 0 18px;
            border-radius: 12px;
            text-decoration: none;
            font-weight: 600;
            border: 1px solid transparent;
        }
        .button-success {
            background: var(--success);
            color: #fff;
        }
        .button-cancel {
            background: #fff;
            color: var(--danger);
            border-color: #f1c6c6;
        }
        .footnote {
            margin-top: 16px;
            font-size: 13px;
            color: var(--muted);
        }
    </style>
</head>
<body>
    <main class="card">
        <span class="eyebrow">Sandbox Provider</span>
        <h1>Mock Payment Gateway</h1>
        <p>This page is only for development and manual QA. You can safely simulate a successful or cancelled payment without using a real provider.</p>
        <section class="meta">
            <strong>Reference Code</strong>
            <div>{{referenceCode}}</div>
            <strong style="margin-top:12px;">Payment Link Id</strong>
            <div>{{encodedPaymentLinkId}}</div>
        </section>
        <section class="actions">
            <a class="button button-success" href="{{successUrl}}">Mark Payment Successful</a>
            <a class="button button-cancel" href="{{cancelUrl}}">Cancel Payment</a>
        </section>
        <p class="footnote">You can remove this route later without affecting the real provider flow.</p>
    </main>
</body>
</html>
""";

        return Content(html, "text/html");
    }

    [HttpGet("sandbox/complete/{referenceCode:long}")]
    public async Task<IActionResult> SandboxComplete(
        long referenceCode,
        [FromQuery] string? paymentLinkId,
        [FromQuery] string? result,
        CancellationToken cancellationToken)
    {
        if (!IsSandboxEnabled())
            return NotFound();

        var code = string.Equals(result, "cancel", StringComparison.OrdinalIgnoreCase) ? "99" : "00";
        var payload = JsonSerializer.Serialize(new
        {
            PaymentLinkId = paymentLinkId,
            ReferenceCode = referenceCode,
            Code = code,
            Signature = "sandbox"
        });

        var command = new ProcessPaymentWebhookCommand(PaymentProviderNames.Sandbox, payload);
        var commandResult = await _sender.Send(command, cancellationToken);

        if (!commandResult.IsSuccess)
            return commandResult.ToActionResult();

        return Ok(new
        {
            message = code == "00" ? "Sandbox payment marked as paid." : "Sandbox payment marked as cancelled.",
            referenceCode,
            paymentLinkId
        });
    }

    private bool IsSandboxEnabled()
    {
        return _sandboxOptions.Enabled && _environment.IsDevelopment();
    }
}
