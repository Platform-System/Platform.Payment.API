using MediatR;
using Microsoft.AspNetCore.Http;
using Platform.Application.Abstractions.Data;
using Platform.Application.Messaging;
using Platform.BuildingBlocks.Responses;
using Platform.Contracts.Messages.Payments;
using Platform.Payment.API.Application.Abstractions.Messaging;
using Platform.Payment.API.Application.Abstractions.Providers;
using Platform.Payment.API.Application.Features.Payments.Mappers;
using Platform.Payment.API.Domain.Enums;
using Platform.Payment.API.Infrastructure.Persistence.Models;

namespace Platform.Payment.API.Application.Features.Payments.Commands.ProcessWebhook;

public sealed class ProcessPaymentWebhookHandler : ICommandHandler<ProcessPaymentWebhookCommand>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IEnumerable<IPaymentProvider> _paymentProviders;
    private readonly IPaymentOutboxWriter _outboxWriter;

    public ProcessPaymentWebhookHandler(
        IUnitOfWork unitOfWork,
        IEnumerable<IPaymentProvider> paymentProviders,
        IPaymentOutboxWriter outboxWriter)
    {
        _unitOfWork = unitOfWork;
        _paymentProviders = paymentProviders;
        _outboxWriter = outboxWriter;
    }

    public async Task<Result<Unit>> Handle(ProcessPaymentWebhookCommand command, CancellationToken cancellationToken)
    {
        var provider = _paymentProviders.FirstOrDefault(x =>
            string.Equals(x.Name, command.Provider, StringComparison.OrdinalIgnoreCase));

        if (provider is null)
            return Result<Unit>.Failure(StatusCodes.Status400BadRequest, "Payment provider is not supported.");

        var webhook = await provider.VerifyWebhookAsync(command.RawBody, cancellationToken);
        if (webhook is null)
            return Result<Unit>.Success(Unit.Value);

        PaymentTransactionModel? paymentModel = null;

        if (!string.IsNullOrWhiteSpace(webhook.PaymentLinkId))
        {
            paymentModel = await _unitOfWork.GetRepository<PaymentTransactionModel>().FindAsync(
                x => x.Provider == provider.Name
                    && x.PaymentLinkId == webhook.PaymentLinkId
                    && x.Status == PaymentStatus.Pending,
                false,
                cancellationToken);
        }

        if (paymentModel is null)
        {
            paymentModel = await _unitOfWork.GetRepository<PaymentTransactionModel>().FindAsync(
                x => x.Provider == provider.Name
                    && x.ReferenceCode == webhook.ReferenceCode
                    && x.Status == PaymentStatus.Pending,
                false,
                cancellationToken);
        }

        if (paymentModel is null)
            return Result<Unit>.Success(Unit.Value);

        var payment = paymentModel.ToDomain();

        if (webhook.Code == "00")
        {
            var markPaidResult = payment.MarkAsPaid();
            if (markPaidResult.IsFailure)
                return Result<Unit>.Success(Unit.Value);

            paymentModel.ApplyDomainState(payment);

            await _outboxWriter.EnqueueAsync(
                new PaymentSucceeded
                {
                    PaymentId = payment.Id,
                    ReferenceType = payment.ReferenceType,
                    ReferenceId = payment.ReferenceId,
                    ReferenceCode = payment.ReferenceCode,
                    Provider = payment.Provider,
                    PaymentLinkId = payment.PaymentLinkId,
                    Amount = payment.Amount,
                    Currency = payment.Currency ?? string.Empty,
                    PaidAt = payment.PaidAt ?? DateTime.UtcNow
                },
                cancellationToken);

            return Result<Unit>.Success(Unit.Value);
        }

        var markCancelledResult = payment.MarkAsCancelled();
        if (markCancelledResult.IsFailure)
            return Result<Unit>.Success(Unit.Value);

        paymentModel.ApplyDomainState(payment);

        await _outboxWriter.EnqueueAsync(
            new PaymentCancelled
            {
                PaymentId = payment.Id,
                ReferenceType = payment.ReferenceType,
                ReferenceId = payment.ReferenceId,
                ReferenceCode = payment.ReferenceCode,
                Provider = payment.Provider,
                PaymentLinkId = payment.PaymentLinkId,
                Amount = payment.Amount,
                Currency = payment.Currency ?? string.Empty,
                ReasonCode = webhook.Code
            },
            cancellationToken);

        return Result<Unit>.Success(Unit.Value);
    }
}
