namespace Platform.Payment.API.Infrastructure.Constants;

public static class ConfigurationSections
{
    public const string PayOS = "PayOS";
    public const string Payment = "Payment";
    public const string Sandbox = "Sandbox";
    public const string PaymentSucceededMessaging = "Messaging:PaymentSucceeded";
    public const string PaymentCancelledMessaging = "Messaging:PaymentCancelled";
    public const string OutboxProcessing = "Messaging:Outbox";
}
