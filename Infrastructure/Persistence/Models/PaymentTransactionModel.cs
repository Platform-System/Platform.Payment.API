using Platform.Domain.Common;
using Platform.Payment.API.Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Payment.API.Infrastructure.Persistence.Models;

[Table("Payments")]
public sealed class PaymentTransactionModel : Entity
{
    public string ReferenceType { get; set; } = string.Empty;
    public Guid ReferenceId { get; set; }
    public long ReferenceCode { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? PaymentLinkId { get; set; }
    public string? CheckoutUrl { get; set; }
    public long Amount { get; set; }
    public string? Currency { get; set; }
    public DateTime? PaidAt { get; set; }
    public PaymentStatus Status { get; set; }
}
