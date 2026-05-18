namespace Platform.Payment.API.Application.Common.Models;

public sealed class PaymentWebhookResult
{
    public string? PaymentLinkId { get; init; }
    public long ReferenceCode { get; init; }
    public string Code { get; init; } = string.Empty;
    public string Signature { get; init; } = string.Empty;
}
