using MediatR;
using Platform.Contracts.Messages.Payments;

namespace Platform.Payment.API.Application.Features.Payments.Notifications;

public sealed class PaymentSucceededNotification : INotification
{
    public PaymentSucceeded Message { get; }

    public PaymentSucceededNotification(PaymentSucceeded message)
    {
        Message = message;
    }
}
