using Platform.BuildingBlocks.DateTimes;
using Platform.Domain.Common;
using Platform.Payment.API.Domain.Enums;
using Platform.Payment.API.Domain.Errors;

namespace Platform.Payment.API.Domain.Entities;

public sealed class PaymentTransaction : AggregateRoot
{
    public string ReferenceType { get; private set; } = string.Empty;
    public Guid ReferenceId { get; private set; }
    public long ReferenceCode { get; private set; }
    public string Provider { get; private set; } = string.Empty;
    public string? PaymentLinkId { get; private set; }
    public string? CheckoutUrl { get; private set; }
    public long Amount { get; private set; }
    public string? Currency { get; private set; }
    public DateTime? PaidAt { get; private set; }
    public PaymentStatus Status { get; private set; }
    public Guid UserId { get; private set; }

    private PaymentTransaction()
    {
    }

    public PaymentTransaction(string referenceType, Guid referenceId, long referenceCode, string provider, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(referenceType))
            throw new InvalidOperationException(PaymentErrors.InvalidReferenceType.Message);

        if (referenceId == Guid.Empty)
            throw new InvalidOperationException(PaymentErrors.InvalidReferenceId.Message);

        if (referenceCode <= 0)
            throw new InvalidOperationException(PaymentErrors.InvalidReferenceCode.Message);

        if (string.IsNullOrWhiteSpace(provider))
            throw new InvalidOperationException(PaymentErrors.InvalidProvider.Message);

        if (userId == Guid.Empty)
            throw new InvalidOperationException("User ID is required.");

        ReferenceType = referenceType;
        ReferenceId = referenceId;
        ReferenceCode = referenceCode;
        Provider = provider;
        Status = PaymentStatus.Pending;
        UserId = userId;
    }

    public static PaymentTransaction Load(
        Guid id,
        string referenceType,
        Guid referenceId,
        long referenceCode,
        string provider,
        string? paymentLinkId,
        string? checkoutUrl,
        long amount,
        string? currency,
        DateTime? paidAt,
        PaymentStatus status,
        Guid userId)
    {
        return new PaymentTransaction
        {
            Id = id,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            ReferenceCode = referenceCode,
            Provider = provider,
            PaymentLinkId = paymentLinkId,
            CheckoutUrl = checkoutUrl,
            Amount = amount,
            Currency = currency,
            PaidAt = paidAt,
            Status = status,
            UserId = userId
        };
    }

    public DomainResult SetCheckout(string paymentLinkId, string checkoutUrl, long amount, string currency)
    {
        if (string.IsNullOrWhiteSpace(paymentLinkId))
            return DomainResult.Failure(PaymentErrors.InvalidPaymentLinkId);

        if (string.IsNullOrWhiteSpace(checkoutUrl))
            return DomainResult.Failure(PaymentErrors.InvalidCheckoutUrl);

        if (amount <= 0)
            return DomainResult.Failure(PaymentErrors.InvalidAmount);

        if (string.IsNullOrWhiteSpace(currency))
            return DomainResult.Failure(PaymentErrors.InvalidCurrency);

        PaymentLinkId = paymentLinkId;
        CheckoutUrl = checkoutUrl;
        Amount = amount;
        Currency = currency;
        return DomainResult.Success();
    }

    public DomainResult MarkAsPaid()
    {
        if (Status != PaymentStatus.Pending)
            return DomainResult.Failure(PaymentErrors.CannotMarkPaid);

        Status = PaymentStatus.Paid;
        PaidAt = Clock.Now;
        return DomainResult.Success();
    }

    public DomainResult MarkAsCancelled()
    {
        if (Status != PaymentStatus.Pending)
            return DomainResult.Failure(PaymentErrors.CannotMarkCancelled);

        Status = PaymentStatus.Cancelled;
        return DomainResult.Success();
    }
}
