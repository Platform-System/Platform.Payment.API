using Platform.Application.Messaging;

namespace Platform.Payment.API.Application.Features.Payments.Commands.ProcessWebhook;

public sealed class ProcessPaymentWebhookCommand : ICommand
{
    public string Provider { get; }
    public string RawBody { get; }

    public ProcessPaymentWebhookCommand(string provider, string rawBody)
    {
        Provider = provider;
        RawBody = rawBody;
    }
}
