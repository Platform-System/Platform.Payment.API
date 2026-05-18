using Platform.Contracts.Payments;
using Platform.Payment.API.Application.Common.Models;

namespace Platform.Payment.API.Application.Abstractions.Providers;

public interface IPaymentProvider
{
    string Name { get; }
    Task<PaymentLinkResponse> CreatePaymentLinkAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default);
    Task CancelPaymentLinkAsync(string? paymentLinkId, long referenceCode, CancellationToken cancellationToken = default);
    Task<PaymentWebhookResult?> VerifyWebhookAsync(string rawBody, CancellationToken cancellationToken = default);
}
