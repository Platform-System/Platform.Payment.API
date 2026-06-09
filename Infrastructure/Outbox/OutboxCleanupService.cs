using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Platform.BuildingBlocks.DateTimes;
using Platform.Payment.API.Infrastructure.Configurations;
using Platform.Payment.API.Infrastructure.Data;

namespace Platform.Payment.API.Infrastructure.Outbox;

public sealed class OutboxCleanupService : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly OutboxProcessingOptions _options;
    private readonly ILogger<OutboxCleanupService> _logger;

    public OutboxCleanupService(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<OutboxProcessingOptions> options,
        ILogger<OutboxCleanupService> logger)
    {
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_options.CleanupIntervalMinutes.GetValueOrDefault());

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment outbox cleanup cycle failed.");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task CleanupBatchAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        var cutoff = Clock.Now.AddDays(-_options.ProcessedRetentionDays.GetValueOrDefault());
        var batchSize = _options.CleanupBatchSize.GetValueOrDefault();

        while (!cancellationToken.IsCancellationRequested)
        {
            var ids = await dbContext.OutboxMessages
                .Where(x => x.ProcessedAt != null && x.ProcessedAt < cutoff)
                .OrderBy(x => x.ProcessedAt)
                .Select(x => x.Id)
                .Take(batchSize)
                .ToListAsync(cancellationToken);

            if (ids.Count == 0)
                break;

            await dbContext.OutboxMessages
                .Where(x => ids.Contains(x.Id))
                .ExecuteDeleteAsync(cancellationToken);
        }
    }
}
