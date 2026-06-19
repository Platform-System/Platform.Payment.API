using Platform.Payment.API.Domain.Entities;
using Platform.Payment.API.Infrastructure.Persistence.Models;

namespace Platform.Payment.API.Application.Features.Payments.Mappers;

public static class PaymentTransactionPersistenceMapper
{
    public static PaymentTransaction ToDomain(this PaymentTransactionModel model)
    {
        return PaymentTransaction.Load(
            model.Id,
            model.ReferenceType,
            model.ReferenceId,
            model.ReferenceCode,
            model.Provider,
            model.PaymentLinkId,
            model.CheckoutUrl,
            model.Amount,
            model.Currency,
            model.PaidAt,
            model.Status,
            model.UserId);
    }

    public static PaymentTransactionModel ToModel(this PaymentTransaction payment)
    {
        return new PaymentTransactionModel
        {
            Id = payment.Id,
            ReferenceType = payment.ReferenceType,
            ReferenceId = payment.ReferenceId,
            ReferenceCode = payment.ReferenceCode,
            Provider = payment.Provider,
            PaymentLinkId = payment.PaymentLinkId,
            CheckoutUrl = payment.CheckoutUrl,
            Amount = payment.Amount,
            Currency = payment.Currency,
            PaidAt = payment.PaidAt,
            Status = payment.Status,
            UserId = payment.UserId
        };
    }

    public static void ApplyDomainState(this PaymentTransactionModel model, PaymentTransaction payment)
    {
        model.ReferenceType = payment.ReferenceType;
        model.ReferenceId = payment.ReferenceId;
        model.ReferenceCode = payment.ReferenceCode;
        model.Provider = payment.Provider;
        model.PaymentLinkId = payment.PaymentLinkId;
        model.CheckoutUrl = payment.CheckoutUrl;
        model.Amount = payment.Amount;
        model.Currency = payment.Currency;
        model.PaidAt = payment.PaidAt;
        model.Status = payment.Status;
        model.UserId = payment.UserId;
    }
}
