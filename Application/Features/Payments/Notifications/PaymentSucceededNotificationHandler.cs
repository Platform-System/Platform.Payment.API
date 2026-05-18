using MediatR;
using Platform.Messaging.Abstractions;

namespace Platform.Payment.API.Application.Features.Payments.Notifications;

public sealed class PaymentSucceededNotificationHandler : INotificationHandler<PaymentSucceededNotification>
{
    private readonly IMessagePublisher _messagePublisher;

    public PaymentSucceededNotificationHandler(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    public async Task Handle(PaymentSucceededNotification notification, CancellationToken cancellationToken)
    {
        await _messagePublisher.PublishAsync(notification.Message, cancellationToken);
    }
}
