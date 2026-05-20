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
        services.AddPlatformRabbitMqMessaging(configuration);
        services.AddScoped<IPaymentOutboxWriter, PaymentOutboxWriter>();
        services.AddHostedService<PaymentOutboxDispatcher>();
        services.AddScoped<IPaymentProvider, PayOSPaymentProvider>();
        services.AddScoped<IPaymentProvider, SandboxPaymentProvider>();

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
