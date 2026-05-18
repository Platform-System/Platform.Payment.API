using Platform.Application.Messaging;
using MediatR;

namespace Platform.Payment.API.Application.Features.Payments.Commands.ProcessWebhook;

public sealed class ProcessPaymentWebhookCommand : ICommand, IHasEvent
{
    public string Provider { get; }
    public string RawBody { get; }
    public List<INotification> Events { get; } = [];

    public ProcessPaymentWebhookCommand(string provider, string rawBody)
    {
        Provider = provider;
        RawBody = rawBody;
    }
}
