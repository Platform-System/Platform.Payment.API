using Platform.Contracts.Payments;
using Platform.Payment.API.Domain.Entities;
using Platform.Payment.API.Infrastructure.Persistence.Models;

namespace Platform.Payment.API.Application.Features.Payments.Mappers;

public static class PaymentTransactionMapper
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
            model.Status);
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
            Status = payment.Status
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
    }

    public static PaymentLinkResponse ToResponse(this PaymentTransactionModel model)
    {
        return new PaymentLinkResponse
        {
            PaymentId = model.Id,
            CheckoutUrl = model.CheckoutUrl,
            PaymentLinkId = model.PaymentLinkId,
            Amount = model.Amount,
            Currency = model.Currency,
            Status = model.Status.ToString()
        };
    }
}
