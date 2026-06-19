using System;

namespace Platform.Payment.API.Application.Features.Payments.Responses;

public sealed class PaymentTransactionResponse
{
    public Guid PaymentId { get; set; }
    public string ReferenceType { get; set; } = string.Empty;
    public Guid ReferenceId { get; set; }
    public long ReferenceCode { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? PaymentLinkId { get; set; }
    public string? CheckoutUrl { get; set; }
    public long Amount { get; set; }
    public string? Currency { get; set; }
    public DateTime? PaidAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
