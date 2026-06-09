using Platform.Common.Grpc;
using Platform.Contracts.Payments;
using Platform.Payment.API.Infrastructure.Persistence.Models;
using Platform.Payment.Grpc;

namespace Platform.Payment.API.Presentation.Grpc;

public static class PaymentIntegrationMapper
{
    public static Platform.Contracts.Payments.CreatePaymentRequest ToContractRequest(this CreatePaymentLinkRequest request)
    {
        return new Platform.Contracts.Payments.CreatePaymentRequest
        {
            ReferenceType = request.ReferenceType,
            ReferenceId = Guid.TryParse(request.ReferenceId, out var referenceId) ? referenceId : Guid.Empty,
            ReferenceCode = request.ReferenceCode,
            Provider = request.Provider,
            Amount = request.Amount,
            Currency = request.Currency,
            Description = request.Description,
            Items = request.Items.Select(item => new CreatePaymentItem
            {
                Name = item.Name,
                Quantity = item.Quantity,
                Price = item.Price
            }).ToList()
        };
    }

    public static CreatePaymentLinkResponse ToSuccessResponse(this Platform.Contracts.Payments.PaymentLinkResponse response)
    {
        return new CreatePaymentLinkResponse
        {
            Status = ResponseStatusExtensions.Success(),
            Data = new PaymentLinkData
            {
                PaymentId = response.PaymentId.ToString(),
                CheckoutUrl = response.CheckoutUrl ?? string.Empty,
                PaymentLinkId = response.PaymentLinkId ?? string.Empty,
                Amount = response.Amount,
                Currency = response.Currency ?? string.Empty,
                Status = response.Status
            }
        };
    }

    public static GetPaymentStatusResponse ToStatusResponse(this PaymentTransactionModel model)
    {
        return new GetPaymentStatusResponse
        {
            Status = ResponseStatusExtensions.Success(),
            Data = new PaymentStatusData
            {
                PaymentId = model.Id.ToString(),
                ReferenceType = model.ReferenceType,
                ReferenceId = model.ReferenceId.ToString(),
                ReferenceCode = model.ReferenceCode,
                Provider = model.Provider,
                PaymentLinkId = model.PaymentLinkId ?? string.Empty,
                Amount = model.Amount,
                Currency = model.Currency ?? string.Empty,
                Status = model.Status.ToString(),
                PaidAt = model.PaidAt?.ToUniversalTime().ToString("O") ?? string.Empty
            }
        };
    }
}
