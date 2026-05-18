using Platform.Domain.Common;

namespace Platform.Payment.API.Domain.Errors;

public static class PaymentErrors
{
    public static Error InvalidReferenceType => new(
        "Payment.InvalidReferenceType",
        "The payment reference type is required.");

    public static Error InvalidReferenceId => new(
        "Payment.InvalidReferenceId",
        "The payment reference id is required.");

    public static Error InvalidReferenceCode => new(
        "Payment.InvalidReferenceCode",
        "The payment reference code must be greater than zero.");

    public static Error InvalidProvider => new(
        "Payment.InvalidProvider",
        "The payment provider is required.");

    public static Error InvalidPaymentLinkId => new(
        "Payment.InvalidPaymentLinkId",
        "The payment link id is required.");

    public static Error InvalidCheckoutUrl => new(
        "Payment.InvalidCheckoutUrl",
        "The checkout url is required.");

    public static Error InvalidAmount => new(
        "Payment.InvalidAmount",
        "The payment amount must be greater than zero.");

    public static Error InvalidCurrency => new(
        "Payment.InvalidCurrency",
        "The payment currency is required.");

    public static Error CannotMarkPaid => new(
        "Payment.CannotMarkPaid",
        "Only pending payments can be marked as paid.");

    public static Error CannotMarkCancelled => new(
        "Payment.CannotMarkCancelled",
        "Only pending payments can be marked as cancelled.");
}
