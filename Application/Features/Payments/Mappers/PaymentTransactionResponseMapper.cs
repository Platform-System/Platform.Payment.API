using Platform.Contracts.Payments;
using Platform.Payment.API.Infrastructure.Persistence.Models;
using Platform.Payment.API.Application.Features.Payments.Responses;

namespace Platform.Payment.API.Application.Features.Payments.Mappers;

public static class PaymentTransactionResponseMapper
{
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

    public static PaymentTransactionResponse ToTransactionResponse(this PaymentTransactionModel model)
    {
        return new PaymentTransactionResponse
        {
            PaymentId = model.Id,
            ReferenceType = model.ReferenceType,
            ReferenceId = model.ReferenceId,
            ReferenceCode = model.ReferenceCode,
            Provider = model.Provider,
            PaymentLinkId = model.PaymentLinkId,
            CheckoutUrl = model.CheckoutUrl,
            Amount = model.Amount,
            Currency = model.Currency,
            PaidAt = model.PaidAt,
            Status = model.Status.ToString(),
            CreatedAt = model.CreatedAt
        };
    }
}
