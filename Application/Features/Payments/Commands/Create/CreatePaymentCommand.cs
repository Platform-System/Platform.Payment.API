using Platform.Application.Messaging;
using Platform.Contracts.Payments;

namespace Platform.Payment.API.Application.Features.Payments.Commands.Create;

public sealed class CreatePaymentCommand : ICommand<PaymentLinkResponse>
{
    public CreatePaymentRequest Request { get; }

    public CreatePaymentCommand(CreatePaymentRequest request)
    {
        Request = request;
    }
}
