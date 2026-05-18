using Grpc.Core;
using MediatR;
using Platform.Payment.API.Application.Features.Payments.Commands.Create;
using Platform.Payment.Grpc;

namespace Platform.Payment.API.Presentation.Grpc;

public sealed class PaymentIntegrationService : PaymentIntegration.PaymentIntegrationBase
{
    private readonly ISender _sender;

    public PaymentIntegrationService(ISender sender)
    {
        _sender = sender;
    }

    public override async Task<CreatePaymentLinkResponse> CreatePaymentLink(
        CreatePaymentLinkRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ReferenceId, out _))
            return PaymentIntegrationResponses.FailureCreatePaymentLink("Invalid reference id.");

        var command = new CreatePaymentCommand(request.ToContractRequest());
        var result = await _sender.Send(command, context.CancellationToken);

        if (!result.IsSuccess)
            return PaymentIntegrationResponses.FailureCreatePaymentLink(result.Errors.FirstOrDefault() ?? "Unable to create payment link.");

        return result.Value.ToSuccessResponse();
    }
}
