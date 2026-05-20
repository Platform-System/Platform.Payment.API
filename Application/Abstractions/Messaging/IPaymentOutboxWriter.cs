using Platform.Contracts.Messages.Payments;

namespace Platform.Payment.API.Application.Abstractions.Messaging;

public interface IPaymentOutboxWriter
{
    Task EnqueueAsync(PaymentSucceeded message, CancellationToken cancellationToken = default);
    Task EnqueueAsync(PaymentCancelled message, CancellationToken cancellationToken = default);
}
