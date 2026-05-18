namespace Platform.Payment.API.Infrastructure.Constants;

public static class ConfigurationValidationMessages
{
    public const string PayOSClientIdRequired = "ClientId required";
    public const string PayOSApiKeyRequired = "ApiKey required";
    public const string PayOSChecksumKeyRequired = "ChecksumKey required";
    public const string PaymentReturnUrlRequired = "ReturnUrl required";
    public const string PaymentCancelUrlRequired = "CancelUrl required";
}
