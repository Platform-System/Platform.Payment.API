using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Platform.BuildingBlocks.Requests;
using Platform.BuildingBlocks.Responses;
using Platform.Contracts.Payments;
using Platform.Payment.API.Application.Features.Payments.Commands.ProcessWebhook;
using Platform.Payment.API.Application.Features.Payments.Queries.GetCurrentUserTransactions;
using Platform.Payment.API.Infrastructure.Configurations;
using Platform.Payment.API.Infrastructure.Providers.Sandbox;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Platform.Payment.API.Presentation.Http;

[Route("api/payments")]
[ApiController]
[Authorize]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly SandboxPaymentOptions _sandboxOptions;
    private readonly SandboxCheckoutPageRenderer _sandboxCheckoutPageRenderer;
    private readonly IWebHostEnvironment _environment;

    public PaymentsController(
        ISender sender,
        IOptions<SandboxPaymentOptions> sandboxOptions,
        SandboxCheckoutPageRenderer sandboxCheckoutPageRenderer,
        IWebHostEnvironment environment)
    {
        _sender = sender;
        _sandboxOptions = sandboxOptions.Value;
        _sandboxCheckoutPageRenderer = sandboxCheckoutPageRenderer;
        _environment = environment;
    }

    /// <summary>
    /// Gets all payment transactions for the logged in user.
    /// </summary>
    [HttpGet("me/transactions")]
    public async Task<IActionResult> GetCurrentUserTransactions([FromQuery] PagingRequest request, CancellationToken cancellationToken)
    {
        var query = new GetCurrentUserTransactionsQuery
        {
            Page = request.Page,
            PageSize = request.PageSize
        };
        var result = await _sender.Send(query, cancellationToken);
        return result.ToActionResult();
    }

    /// <summary>
    /// Handles a payment webhook for the given provider.
    /// </summary>
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

    /// <summary>
    /// Shows the sandbox checkout page.
    /// </summary>
    [HttpGet("sandbox/checkout/{referenceCode:long}")]
    [AllowAnonymous]
    public IActionResult SandboxCheckout(long referenceCode, [FromQuery] string? paymentLinkId)
    {
        if (!IsSandboxEnabled())
            return NotFound();

        var html = _sandboxCheckoutPageRenderer.Render(referenceCode, paymentLinkId);
        return Content(html, "text/html");
    }

    /// <summary>
    /// Completes a sandbox payment flow.
    /// </summary>
    [HttpGet("sandbox/complete/{referenceCode:long}")]
    [AllowAnonymous]
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
