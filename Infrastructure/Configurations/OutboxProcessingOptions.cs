namespace Platform.Payment.API.Infrastructure.Configurations;

public sealed class OutboxProcessingOptions
{
    public int? DispatchIntervalSeconds { get; set; }
    public int? BaseRetryDelaySeconds { get; set; }
    public int? MaxRetryDelaySeconds { get; set; }
    public int? CleanupIntervalMinutes { get; set; }
    public int? ProcessedRetentionDays { get; set; }
    public int? CleanupBatchSize { get; set; }
}
