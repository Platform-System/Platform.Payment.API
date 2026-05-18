using MediatR;
using Platform.Contracts.Messages.Payments;

namespace Platform.Payment.API.Application.Features.Payments.Notifications;

public sealed class PaymentCancelledNotification : INotification
{
    public PaymentCancelled Message { get; }

    public PaymentCancelledNotification(PaymentCancelled message)
    {
        Message = message;
    }
}
