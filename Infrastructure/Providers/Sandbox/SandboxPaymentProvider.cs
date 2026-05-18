using System.Text.Json;
using Microsoft.Extensions.Options;
using Platform.Contracts.Payments;
using Platform.Payment.API.Application.Abstractions.Providers;
using Platform.Payment.API.Application.Common.Models;
using Platform.Payment.API.Infrastructure.Configurations;

namespace Platform.Payment.API.Infrastructure.Providers.Sandbox;

public sealed class SandboxPaymentProvider : IPaymentProvider
{
    private readonly SandboxPaymentOptions _options;

    public string Name => PaymentProviderNames.Sandbox;

    public SandboxPaymentProvider(IOptions<SandboxPaymentOptions> options)
    {
        _options = options.Value;
    }

    public Task<PaymentLinkResponse> CreatePaymentLinkAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var paymentLinkId = $"{Name}-{request.ReferenceCode}-{Guid.NewGuid():N}";
        var baseUrl = string.IsNullOrWhiteSpace(_options.PublicBaseUrl)
            ? "http://localhost:8080"
            : _options.PublicBaseUrl.TrimEnd('/');

        var checkoutUrl = $"{baseUrl}/api/payments/sandbox/checkout/{request.ReferenceCode}?paymentLinkId={Uri.EscapeDataString(paymentLinkId)}";

        return Task.FromResult(new PaymentLinkResponse
        {
            CheckoutUrl = checkoutUrl,
            PaymentLinkId = paymentLinkId,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = "Pending"
        });
    }

    public Task<PaymentWebhookResult?> VerifyWebhookAsync(string rawBody, CancellationToken cancellationToken = default)
    {
        var payload = JsonSerializer.Deserialize<PaymentWebhookResult>(rawBody);
        return Task.FromResult<PaymentWebhookResult?>(payload);
    }

    public Task CancelPaymentLinkAsync(string? paymentLinkId, long referenceCode, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }
}
