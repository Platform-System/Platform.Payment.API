using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Contracts.Messages.Payments;
using Platform.Messaging.Abstractions;
using Platform.Payment.API.Infrastructure.Data;
using Platform.Payment.API.Infrastructure.Persistence.Models;

namespace Platform.Payment.API.Infrastructure.Outbox;

public sealed class PaymentOutboxDispatcher : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PaymentOutboxDispatcher> _logger;

    public PaymentOutboxDispatcher(IServiceScopeFactory serviceScopeFactory, ILogger<PaymentOutboxDispatcher> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DispatchBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment outbox dispatch cycle failed.");
            }

            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }

    private async Task DispatchBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var messagePublisher = scope.ServiceProvider.GetRequiredService<IMessagePublisher>();

        var messages = await dbContext.OutboxMessages
            .Where(x => x.ProcessedAt == null)
            .OrderBy(x => x.CreatedAt)
            .Take(20)
            .ToListAsync(cancellationToken);

        if (messages.Count == 0)
            return;

        foreach (var message in messages)
        {
            try
            {
                await PublishAsync(messagePublisher, message, cancellationToken);
                message.ProcessedAt = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.Error = ex.Message;
                _logger.LogWarning(ex, "Payment outbox message {OutboxMessageId} failed to publish.", message.Id);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task PublishAsync(
        IMessagePublisher messagePublisher,
        OutboxMessageModel outboxMessage,
        CancellationToken cancellationToken)
    {
        switch (outboxMessage.MessageType)
        {
            case PaymentOutboxMessageTypes.PaymentSucceeded:
                var succeeded = JsonSerializer.Deserialize<PaymentSucceeded>(outboxMessage.Payload, JsonOptions)
                    ?? throw new InvalidOperationException("PaymentSucceeded payload is invalid.");
                await messagePublisher.PublishAsync(succeeded, cancellationToken);
                break;

            case PaymentOutboxMessageTypes.PaymentCancelled:
                var cancelled = JsonSerializer.Deserialize<PaymentCancelled>(outboxMessage.Payload, JsonOptions)
                    ?? throw new InvalidOperationException("PaymentCancelled payload is invalid.");
                await messagePublisher.PublishAsync(cancelled, cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"Unsupported outbox message type '{outboxMessage.MessageType}'.");
        }
    }
}
