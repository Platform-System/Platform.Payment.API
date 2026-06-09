namespace Platform.Payment.API.Infrastructure.Constants;

public static class ConfigurationValidationMessages
{
    public const string PayOSClientIdRequired = "ClientId required";
    public const string PayOSApiKeyRequired = "ApiKey required";
    public const string PayOSChecksumKeyRequired = "ChecksumKey required";
    public const string PaymentReturnUrlRequired = "ReturnUrl required";
    public const string PaymentCancelUrlRequired = "CancelUrl required";
    public const string PaymentSucceededTopicRequired = "Messaging:PaymentSucceeded:Topic is required.";
    public const string PaymentSucceededDeadLetterTopicRequired = "Messaging:PaymentSucceeded:DeadLetterTopic is required.";
    public const string PaymentSucceededMaxRetryCountInvalid = "Messaging:PaymentSucceeded:MaxRetryCount must be greater than zero.";
    public const string PaymentCancelledTopicRequired = "Messaging:PaymentCancelled:Topic is required.";
    public const string PaymentCancelledDeadLetterTopicRequired = "Messaging:PaymentCancelled:DeadLetterTopic is required.";
    public const string PaymentCancelledMaxRetryCountInvalid = "Messaging:PaymentCancelled:MaxRetryCount must be greater than zero.";
    public const string OutboxDispatchIntervalInvalid = "Messaging:Outbox:DispatchIntervalSeconds must be greater than zero.";
    public const string OutboxBaseRetryDelayInvalid = "Messaging:Outbox:BaseRetryDelaySeconds must be greater than zero.";
    public const string OutboxMaxRetryDelayInvalid = "Messaging:Outbox:MaxRetryDelaySeconds must be greater than zero.";
    public const string OutboxCleanupIntervalInvalid = "Messaging:Outbox:CleanupIntervalMinutes must be greater than zero.";
    public const string OutboxProcessedRetentionInvalid = "Messaging:Outbox:ProcessedRetentionDays must be greater than zero.";
    public const string OutboxCleanupBatchSizeInvalid = "Messaging:Outbox:CleanupBatchSize must be greater than zero.";
    public const string OutboxRetryDelayRangeInvalid = "Messaging:Outbox:MaxRetryDelaySeconds must be greater than or equal to BaseRetryDelaySeconds.";
}
