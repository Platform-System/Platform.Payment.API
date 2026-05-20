namespace Platform.Payment.API.Infrastructure.Outbox;

public static class PaymentOutboxMessageTypes
{
    public const string PaymentSucceeded = "payments.succeeded";
    public const string PaymentCancelled = "payments.cancelled";
}
