using Platform.Contracts.Messages.Payments;
using Platform.Messaging.Models;

namespace Platform.Payment.API.Infrastructure.Messaging;

public sealed class PaymentSucceededDeadLetterEnvelope : KafkaDeadLetterEnvelope<PaymentSucceeded>
{
}
