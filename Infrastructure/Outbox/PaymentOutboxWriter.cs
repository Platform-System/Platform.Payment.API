using System.Text.Json;
using Platform.Application.Abstractions.Data;
using Platform.Contracts.Messages.Payments;
using Platform.Payment.API.Application.Abstractions.Messaging;
using Platform.Payment.API.Infrastructure.Persistence.Models;

namespace Platform.Payment.API.Infrastructure.Outbox;

public sealed class PaymentOutboxWriter : IPaymentOutboxWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IUnitOfWork _unitOfWork;

    public PaymentOutboxWriter(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public Task EnqueueAsync(PaymentSucceeded message, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(PaymentOutboxMessageTypes.PaymentSucceeded, message, cancellationToken);
    }

    public Task EnqueueAsync(PaymentCancelled message, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(PaymentOutboxMessageTypes.PaymentCancelled, message, cancellationToken);
    }

    private async Task EnqueueAsync<TMessage>(string messageType, TMessage message, CancellationToken cancellationToken)
    {
        var outboxMessage = new OutboxMessageModel
        {
            MessageType = messageType,
            Payload = JsonSerializer.Serialize(message, JsonOptions)
        };

        await _unitOfWork.GetRepository<OutboxMessageModel>().AddAsync(outboxMessage, cancellationToken);
    }
}
