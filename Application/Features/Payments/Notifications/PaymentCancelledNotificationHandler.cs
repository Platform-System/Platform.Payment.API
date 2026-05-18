using MediatR;
using Platform.Messaging.Abstractions;

namespace Platform.Payment.API.Application.Features.Payments.Notifications;

public sealed class PaymentCancelledNotificationHandler : INotificationHandler<PaymentCancelledNotification>
{
    private readonly IMessagePublisher _messagePublisher;

    public PaymentCancelledNotificationHandler(IMessagePublisher messagePublisher)
    {
        _messagePublisher = messagePublisher;
    }

    public async Task Handle(PaymentCancelledNotification notification, CancellationToken cancellationToken)
    {
        await _messagePublisher.PublishAsync(notification.Message, cancellationToken);
    }
}
