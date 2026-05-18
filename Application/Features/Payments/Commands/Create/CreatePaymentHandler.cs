using Microsoft.AspNetCore.Http;
using Platform.Application.Abstractions.Data;
using Platform.Application.Messaging;
using Platform.BuildingBlocks.Responses;
using Platform.Contracts.Payments;
using Platform.Payment.API.Application.Abstractions.Providers;
using Platform.Payment.API.Application.Features.Payments.Mappers;
using Platform.Payment.API.Domain.Entities;
using Platform.Payment.API.Domain.Enums;
using Platform.Payment.API.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;

namespace Platform.Payment.API.Application.Features.Payments.Commands.Create;

public sealed class CreatePaymentHandler : ICommandHandler<CreatePaymentCommand, PaymentLinkResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnumerable<IPaymentProvider> _paymentProviders;

    public CreatePaymentHandler(IUnitOfWork unitOfWork, IEnumerable<IPaymentProvider> paymentProviders)
    {
        _unitOfWork = unitOfWork;
        _paymentProviders = paymentProviders;
    }

    public async Task<Result<PaymentLinkResponse>> Handle(CreatePaymentCommand command, CancellationToken cancellationToken)
    {
        var repository = _unitOfWork.GetRepository<PaymentTransactionModel>();
        var pendingPayments = await repository
            .GetQueryable()
            .Where(x => x.ReferenceType == command.Request.ReferenceType
                && x.ReferenceId == command.Request.ReferenceId
                && x.Status == PaymentStatus.Pending)
            .ToListAsync(cancellationToken);

        var existingPaymentModel = pendingPayments.FirstOrDefault(x =>
            string.Equals(x.Provider, command.Request.Provider, StringComparison.OrdinalIgnoreCase));

        if (existingPaymentModel is not null && !string.IsNullOrWhiteSpace(existingPaymentModel.CheckoutUrl))
        {
            return Result<PaymentLinkResponse>.Success(existingPaymentModel.ToResponse());
        }

        var provider = _paymentProviders.FirstOrDefault(x =>
            string.Equals(x.Name, command.Request.Provider, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
            return Result<PaymentLinkResponse>.Failure(StatusCodes.Status400BadRequest, "Payment provider is not supported.");

        foreach (var pendingPayment in pendingPayments.Where(x =>
                     !string.Equals(x.Provider, command.Request.Provider, StringComparison.OrdinalIgnoreCase)))
        {
            var staleProvider = _paymentProviders.FirstOrDefault(x =>
                string.Equals(x.Name, pendingPayment.Provider, StringComparison.OrdinalIgnoreCase));

            if (staleProvider is null)
            {
                return Result<PaymentLinkResponse>.Failure(
                    StatusCodes.Status500InternalServerError,
                    $"Payment provider '{pendingPayment.Provider}' is not configured.");
            }

            try
            {
                await staleProvider.CancelPaymentLinkAsync(
                    pendingPayment.PaymentLinkId,
                    pendingPayment.ReferenceCode,
                    cancellationToken);
            }
            catch
            {
                return Result<PaymentLinkResponse>.Failure(
                    StatusCodes.Status502BadGateway,
                    $"Unable to cancel the existing {pendingPayment.Provider} payment link.");
            }

            var stalePayment = pendingPayment.ToDomain();
            var cancelResult = stalePayment.MarkAsCancelled();
            if (!cancelResult.IsFailure)
            {
                pendingPayment.ApplyDomainState(stalePayment);
            }
        }

        var paymentLink = await provider.CreatePaymentLinkAsync(command.Request, cancellationToken);

        if (string.IsNullOrWhiteSpace(paymentLink.CheckoutUrl)
            || string.IsNullOrWhiteSpace(paymentLink.PaymentLinkId)
            || string.IsNullOrWhiteSpace(paymentLink.Currency))
        {
            return Result<PaymentLinkResponse>.Failure(StatusCodes.Status502BadGateway, "Unable to create a payment link.");
        }

        var payment = existingPaymentModel?.ToDomain() ?? new PaymentTransaction(
            command.Request.ReferenceType,
            command.Request.ReferenceId,
            command.Request.ReferenceCode,
            provider.Name);

        var setCheckoutResult = payment.SetCheckout(
            paymentLink.PaymentLinkId,
            paymentLink.CheckoutUrl,
            paymentLink.Amount,
            paymentLink.Currency);

        if (setCheckoutResult.IsFailure)
            return Result<PaymentLinkResponse>.Failure(StatusCodes.Status400BadRequest, "Unable to persist payment information.");

        if (existingPaymentModel is null)
        {
            await repository.AddAsync(payment.ToModel(), cancellationToken);
            paymentLink.PaymentId = payment.Id;
        }
        else
        {
            existingPaymentModel.ApplyDomainState(payment);
            paymentLink.PaymentId = existingPaymentModel.Id;
        }

        return Result<PaymentLinkResponse>.Success(paymentLink);
    }
}
