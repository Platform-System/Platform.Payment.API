using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Platform.BuildingBlocks.DateTimes;
using Platform.Contracts.Messages.Payments;
using Platform.Messaging.Abstractions;
using Platform.Messaging.Configurations;
using Platform.Messaging.Helpers;
using Platform.Messaging.Hosting;
using Platform.Payment.API.Infrastructure.Data;
using Platform.Payment.API.Infrastructure.Configurations;
using Platform.Payment.API.Infrastructure.Messaging;
using Platform.Payment.API.Infrastructure.Persistence.Models;

namespace Platform.Payment.API.Infrastructure.Outbox;

public sealed class PaymentOutboxDispatcher : KafkaOutboxDispatcherBase<ClaimedOutboxMessage>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly TimeSpan DispatchLeaseDuration = TimeSpan.FromMinutes(5);
    private static readonly KafkaOutboxMessageTypeRegistry<OutboxPublishContext> PublishRegistry = CreatePublishRegistry();
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly OutboxProcessingOptions _processingOptions;
    private readonly PaymentSucceededOptions _paymentSucceededOptions;
    private readonly PaymentCancelledOptions _paymentCancelledOptions;
    private readonly ILogger<PaymentOutboxDispatcher> _logger;

    public PaymentOutboxDispatcher(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<PaymentSucceededOptions> paymentSucceededOptions,
        IOptions<PaymentCancelledOptions> paymentCancelledOptions,
        IOptions<OutboxProcessingOptions> processingOptions,
        ILogger<PaymentOutboxDispatcher> logger)
        : base(
            processingOptions.Value.DispatchIntervalSeconds.GetValueOrDefault(),
            Math.Max(
                paymentSucceededOptions.Value.MaxRetryCount.GetValueOrDefault(),
                paymentCancelledOptions.Value.MaxRetryCount.GetValueOrDefault()),
            logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _paymentSucceededOptions = paymentSucceededOptions.Value;
        _paymentCancelledOptions = paymentCancelledOptions.Value;
        _processingOptions = processingOptions.Value;
        _logger = logger;
    }

    private Task DispatchBatchAsync(CancellationToken cancellationToken)
        => DispatchBatchAsyncCore(cancellationToken);

    protected override async Task<IReadOnlyCollection<ClaimedOutboxMessage>> ClaimDueMessagesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        return await ClaimDueMessagesAsync(dbContext, batchSize: 20, cancellationToken);
    }

    protected override int GetRetryCount(ClaimedOutboxMessage message) => message.RetryCount;

    protected override OutboxRetryConfiguration GetRetryConfiguration(ClaimedOutboxMessage message)
        => message.MessageType switch
        {
            PaymentOutboxMessageTypes.PaymentSucceeded => OutboxRetryConfiguration.Create(_paymentSucceededOptions.MaxRetryCount.GetValueOrDefault()),
            PaymentOutboxMessageTypes.PaymentCancelled => OutboxRetryConfiguration.Create(_paymentCancelledOptions.MaxRetryCount.GetValueOrDefault()),
            _ => base.GetRetryConfiguration(message)
        };

    protected override void SetRetryCount(ClaimedOutboxMessage message, int retryCount) => message.RetryCount = retryCount;

    protected override async Task PublishAsync(ClaimedOutboxMessage message, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IKafkaMessagePublisher>();
        await PublishRegistry.PublishAsync(message.MessageType, CreateContext(publisher, message), cancellationToken);
    }

    protected override async Task PublishDeadLetterAsync(ClaimedOutboxMessage message, string error, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IKafkaMessagePublisher>();
        await PublishRegistry.PublishDeadLetterAsync(message.MessageType, CreateContext(publisher, message), error, cancellationToken);
    }

    protected override async Task MarkProcessedAsync(ClaimedOutboxMessage message, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await MarkProcessedAsync(dbContext, message.Id, cancellationToken);
    }

    protected override async Task<DateTime> ScheduleRetryAsync(
        ClaimedOutboxMessage message,
        int retryCount,
        string error,
        CancellationToken cancellationToken)
    {
        var nextRetryAt = Clock.Now.Add(RetryDelayCalculator.Calculate(
            retryCount,
            _processingOptions.BaseRetryDelaySeconds.GetValueOrDefault(),
            _processingOptions.MaxRetryDelaySeconds.GetValueOrDefault()));

        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await ScheduleRetryAsync(dbContext, message.Id, retryCount, error, nextRetryAt, cancellationToken);
        return nextRetryAt;
    }

    protected override void OnRetryScheduled(ClaimedOutboxMessage message, int retryCount, DateTime nextRetryAt, Exception exception)
    {
        _logger.LogWarning(
            exception,
            "Payment outbox message {OutboxMessageId} failed to publish. RetryCount={RetryCount}. NextRetryAt={NextRetryAt}.",
            message.Id,
            retryCount,
            nextRetryAt);
    }

    protected override void OnDeadLetterPublishFailed(ClaimedOutboxMessage message, DateTime nextRetryAt, Exception exception)
    {
        _logger.LogWarning(
            exception,
            "Payment outbox message {OutboxMessageId} failed to publish to dead-letter topic. NextRetryAt={NextRetryAt}.",
            message.Id,
            nextRetryAt);
    }

    private OutboxPublishContext CreateContext(IKafkaMessagePublisher publisher, ClaimedOutboxMessage message)
        => message.MessageType switch
        {
            PaymentOutboxMessageTypes.PaymentSucceeded => new OutboxPublishContext(publisher, _paymentSucceededOptions, message),
            PaymentOutboxMessageTypes.PaymentCancelled => new OutboxPublishContext(publisher, _paymentCancelledOptions, message),
            _ => throw new InvalidOperationException($"Unsupported outbox message type '{message.MessageType}'.")
        };

    private static async Task<List<ClaimedOutboxMessage>> ClaimDueMessagesAsync(
        PaymentDbContext dbContext,
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (!dbContext.Database.IsRelational())
        {
            var now = Clock.Now;
            var fallbackMessages = await dbContext.OutboxMessages
                .Where(x => x.ProcessedAt == null && (x.NextRetryAt == null || x.NextRetryAt <= now))
                .OrderBy(x => x.CreatedAt)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (fallbackMessages.Count == 0)
                return [];

            var leaseUntil = now.Add(DispatchLeaseDuration);
            foreach (var message in fallbackMessages)
                message.NextRetryAt = leaseUntil;

            await dbContext.SaveChangesAsync(cancellationToken);

            return fallbackMessages
                .Select(message => new ClaimedOutboxMessage
                {
                    Id = message.Id,
                    MessageType = message.MessageType,
                    Payload = message.Payload,
                    ProcessedAt = message.ProcessedAt,
                    NextRetryAt = message.NextRetryAt,
                    Error = message.Error,
                    RetryCount = message.RetryCount,
                    CreatedAt = message.CreatedAt
                })
                .ToList();
        }

        var currentTime = Clock.Now;
        return await RelationalLeaseClaimHelper.ClaimBatchAsync(
            dbContext,
            """
            UPDATE "OutboxMessages" AS outbox
            SET "NextRetryAt" = @leaseUntil,
                "UpdatedAt" = @now,
                "UpdatedBy" = @updatedBy
            WHERE outbox."Id" IN (
                SELECT candidate."Id"
                FROM "OutboxMessages" AS candidate
                WHERE candidate."ProcessedAt" IS NULL
                  AND (candidate."NextRetryAt" IS NULL OR candidate."NextRetryAt" <= @now)
                ORDER BY candidate."CreatedAt"
                FOR UPDATE SKIP LOCKED
                LIMIT @batchSize
            )
            RETURNING outbox."Id", outbox."MessageType", outbox."Payload", outbox."ProcessedAt", outbox."NextRetryAt", outbox."Error", outbox."RetryCount", outbox."CreatedAt";
            """,
            command =>
            {
                DbCommandParameterHelper.AddParameter(command, "@leaseUntil", currentTime.Add(DispatchLeaseDuration));
                DbCommandParameterHelper.AddParameter(command, "@now", currentTime);
                DbCommandParameterHelper.AddParameter(command, "@updatedBy", "system");
                DbCommandParameterHelper.AddParameter(command, "@batchSize", batchSize);
            },
            reader => new ClaimedOutboxMessage
            {
                Id = reader.GetGuid(0),
                MessageType = reader.GetString(1),
                Payload = reader.GetString(2),
                ProcessedAt = reader.IsDBNull(3) ? null : reader.GetDateTime(3),
                NextRetryAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
                Error = reader.IsDBNull(5) ? null : reader.GetString(5),
                RetryCount = reader.GetInt32(6),
                CreatedAt = reader.GetDateTime(7)
            },
            cancellationToken);
    }

    private static Task MarkProcessedAsync(
        PaymentDbContext dbContext,
        Guid outboxMessageId,
        CancellationToken cancellationToken)
        => EntityMutationHelper.UpdateAsync(
            dbContext,
            dbContext.OutboxMessages.Where(x => x.Id == outboxMessageId),
            setters => setters
                .SetProperty(x => x.ProcessedAt, Clock.Now)
                .SetProperty(x => x.NextRetryAt, (DateTime?)null)
                .SetProperty(x => x.Error, (string?)null),
            token => dbContext.OutboxMessages.FirstOrDefaultAsync(x => x.Id == outboxMessageId, token),
            message =>
            {
                message.ProcessedAt = Clock.Now;
                message.NextRetryAt = null;
                message.Error = null;
            },
            cancellationToken);

    private static Task ScheduleRetryAsync(
        PaymentDbContext dbContext,
        Guid outboxMessageId,
        int retryCount,
        string error,
        DateTime nextRetryAt,
        CancellationToken cancellationToken)
        => EntityMutationHelper.UpdateAsync(
            dbContext,
            dbContext.OutboxMessages.Where(x => x.Id == outboxMessageId),
            setters => setters
                .SetProperty(x => x.RetryCount, retryCount)
                .SetProperty(x => x.Error, error)
                .SetProperty(x => x.NextRetryAt, nextRetryAt),
            token => dbContext.OutboxMessages.FirstOrDefaultAsync(x => x.Id == outboxMessageId, token),
            message =>
            {
                message.RetryCount = retryCount;
                message.Error = error;
                message.NextRetryAt = nextRetryAt;
            },
            cancellationToken);

    private static KafkaOutboxMessageTypeRegistry<OutboxPublishContext> CreatePublishRegistry()
    {
        return new KafkaOutboxMessageTypeRegistry<OutboxPublishContext>()
            .Register(
                PaymentOutboxMessageTypes.PaymentSucceeded,
                PublishPaymentSucceededAsync,
                PublishPaymentSucceededDeadLetterAsync)
            .Register(
                PaymentOutboxMessageTypes.PaymentCancelled,
                PublishPaymentCancelledAsync,
                PublishPaymentCancelledDeadLetterAsync);
    }

    private static async Task PublishPaymentSucceededAsync(OutboxPublishContext context, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<PaymentSucceeded>(context.OutboxMessage.Payload, JsonOptions)
            ?? throw new InvalidOperationException("PaymentSucceeded payload is invalid.");
        await context.Publisher.PublishAsync(context.Options.Topic, message.ReferenceId.ToString(), message, cancellationToken);
    }

    private static async Task PublishPaymentCancelledAsync(OutboxPublishContext context, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<PaymentCancelled>(context.OutboxMessage.Payload, JsonOptions)
            ?? throw new InvalidOperationException("PaymentCancelled payload is invalid.");
        await context.Publisher.PublishAsync(context.Options.Topic, message.ReferenceId.ToString(), message, cancellationToken);
    }

    private static async Task PublishPaymentSucceededDeadLetterAsync(
        OutboxPublishContext context,
        string error,
        CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<PaymentSucceeded>(context.OutboxMessage.Payload, JsonOptions)
            ?? throw new InvalidOperationException("PaymentSucceeded payload is invalid.");

        await context.Publisher.PublishAsync(
            context.Options.DeadLetterTopic,
            message.ReferenceId.ToString(),
            KafkaEnvelopeFactory.CreateDeadLetterEnvelope<PaymentSucceededDeadLetterEnvelope, PaymentSucceeded>(
                message,
                context.OutboxMessage.MessageType,
                context.OutboxMessage.RetryCount,
                error,
                message.PaidAt,
                Clock.Now,
                context.OutboxMessage.Id),
            cancellationToken);
    }

    private static async Task PublishPaymentCancelledDeadLetterAsync(
        OutboxPublishContext context,
        string error,
        CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<PaymentCancelled>(context.OutboxMessage.Payload, JsonOptions)
            ?? throw new InvalidOperationException("PaymentCancelled payload is invalid.");

        await context.Publisher.PublishAsync(
            context.Options.DeadLetterTopic,
            message.ReferenceId.ToString(),
            KafkaEnvelopeFactory.CreateDeadLetterEnvelope<PaymentCancelledDeadLetterEnvelope, PaymentCancelled>(
                message,
                context.OutboxMessage.MessageType,
                context.OutboxMessage.RetryCount,
                error,
                Clock.Now,
                Clock.Now,
                context.OutboxMessage.Id),
            cancellationToken);
    }
}

public sealed class ClaimedOutboxMessage
{
    public Guid Id { get; init; }
    public string MessageType { get; init; } = string.Empty;
    public string Payload { get; init; } = string.Empty;
    public DateTime? ProcessedAt { get; init; }
    public DateTime? NextRetryAt { get; init; }
    public string? Error { get; init; }
    public int RetryCount { get; set; }
    public DateTime CreatedAt { get; init; }
}

internal sealed record OutboxPublishContext(
    IKafkaMessagePublisher Publisher,
    Platform.Messaging.Configurations.KafkaTopicRetryOptions Options,
    ClaimedOutboxMessage OutboxMessage);
