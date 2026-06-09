using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.DependencyInjection;
using Platform.Messaging.DependencyInjection;
using Platform.Payment.API.Application.Abstractions.Providers;
using Platform.Payment.API.Application.Abstractions.Messaging;
using Platform.Payment.API.Infrastructure.Configurations;
using Platform.Payment.API.Infrastructure.Constants;
using Platform.Payment.API.Infrastructure.Data;
using Platform.Payment.API.Infrastructure.Outbox;
using Platform.Payment.API.Infrastructure.Providers.PayOS;
using Platform.Payment.API.Infrastructure.Providers.Sandbox;

namespace Platform.Payment.API.Infrastructure.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddPaymentInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("PaymentDb");

        services.AddInfrastructure(configuration);
        services.AddDbContext<PaymentDbContext>(options =>
        {
            options.UseNpgsql(connectionString);
        });

        services.AddScoped<BaseDbContext>(sp => sp.GetRequiredService<PaymentDbContext>());
        services.AddKafkaMessaging(configuration);
        services.AddOptions<PaymentSucceededOptions>()
            .Bind(configuration.GetSection(ConfigurationSections.PaymentSucceededMessaging))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Topic), ConfigurationValidationMessages.PaymentSucceededTopicRequired)
            .Validate(options => !string.IsNullOrWhiteSpace(options.DeadLetterTopic), ConfigurationValidationMessages.PaymentSucceededDeadLetterTopicRequired)
            .Validate(options => options.MaxRetryCount is > 0, ConfigurationValidationMessages.PaymentSucceededMaxRetryCountInvalid)
            .ValidateOnStart();
        services.AddOptions<PaymentCancelledOptions>()
            .Bind(configuration.GetSection(ConfigurationSections.PaymentCancelledMessaging))
            .Validate(options => !string.IsNullOrWhiteSpace(options.Topic), ConfigurationValidationMessages.PaymentCancelledTopicRequired)
            .Validate(options => !string.IsNullOrWhiteSpace(options.DeadLetterTopic), ConfigurationValidationMessages.PaymentCancelledDeadLetterTopicRequired)
            .Validate(options => options.MaxRetryCount is > 0, ConfigurationValidationMessages.PaymentCancelledMaxRetryCountInvalid)
            .ValidateOnStart();
        services.AddOptions<OutboxProcessingOptions>()
            .Bind(configuration.GetSection(ConfigurationSections.OutboxProcessing))
            .Validate(options => options.DispatchIntervalSeconds is > 0, ConfigurationValidationMessages.OutboxDispatchIntervalInvalid)
            .Validate(options => options.BaseRetryDelaySeconds is > 0, ConfigurationValidationMessages.OutboxBaseRetryDelayInvalid)
            .Validate(options => options.MaxRetryDelaySeconds is > 0, ConfigurationValidationMessages.OutboxMaxRetryDelayInvalid)
            .Validate(options => options.CleanupIntervalMinutes is > 0, ConfigurationValidationMessages.OutboxCleanupIntervalInvalid)
            .Validate(options => options.ProcessedRetentionDays is > 0, ConfigurationValidationMessages.OutboxProcessedRetentionInvalid)
            .Validate(options => options.CleanupBatchSize is > 0, ConfigurationValidationMessages.OutboxCleanupBatchSizeInvalid)
            .Validate(options => options.MaxRetryDelaySeconds >= options.BaseRetryDelaySeconds, ConfigurationValidationMessages.OutboxRetryDelayRangeInvalid)
            .ValidateOnStart();
        services.AddScoped<IPaymentOutboxWriter, PaymentOutboxWriter>();
        services.AddHostedService<PaymentOutboxDispatcher>();
        services.AddHostedService<OutboxCleanupService>();
        services.AddScoped<IPaymentProvider, PayOSPaymentProvider>();
        services.AddScoped<IPaymentProvider, SandboxPaymentProvider>();
        services.AddSingleton<SandboxCheckoutPageRenderer>();

        services.AddOptions<PayOSClientOptions>()
            .Bind(configuration.GetSection(ConfigurationSections.PayOS))
            .ValidateOnStart();

        services.AddOptions<PaymentSettings>()
            .Bind(configuration.GetSection(ConfigurationSections.Payment))
            .ValidateOnStart();
        services.AddOptions<SandboxPaymentOptions>()
            .Bind(configuration.GetSection(ConfigurationSections.Sandbox))
            .Configure(options =>
            {
                if (string.IsNullOrWhiteSpace(options.PublicBaseUrl))
                {
                    options.PublicBaseUrl = configuration.GetSection(ConfigurationSections.Payment).GetValue<string>(nameof(PaymentSettings.PublicBaseUrl)) ?? string.Empty;
                }
            });

        return services;
    }
}
