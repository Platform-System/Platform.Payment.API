using Platform.Domain.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Platform.Payment.API.Infrastructure.Persistence.Models;

[Table("OutboxMessages")]
public sealed class OutboxMessageModel : Entity
{
    public string MessageType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public DateTime? ProcessedAt { get; set; }
    public string? Error { get; set; }
    public int RetryCount { get; set; }
}
