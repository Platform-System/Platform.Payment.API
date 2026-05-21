using System.Net;

namespace Platform.Payment.API.Infrastructure.Providers.Sandbox;

public sealed class SandboxCheckoutPageRenderer
{
    public string Render(long referenceCode, string? paymentLinkId)
    {
        var encodedPaymentLinkId = WebUtility.HtmlEncode(paymentLinkId ?? string.Empty);
        var successUrl = $"/api/payments/sandbox/complete/{referenceCode}?paymentLinkId={Uri.EscapeDataString(paymentLinkId ?? string.Empty)}&result=success";
        var cancelUrl = $"/api/payments/sandbox/complete/{referenceCode}?paymentLinkId={Uri.EscapeDataString(paymentLinkId ?? string.Empty)}&result=cancel";

        return $$"""
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
    }
}
