using Platform.Common.Grpc;
using Platform.Payment.Grpc;

namespace Platform.Payment.API.Presentation.Grpc;

public static class PaymentIntegrationResponses
{
    public static CreatePaymentLinkResponse FailureCreatePaymentLink(string errorMessage)
        => new()
        {
            Status = ResponseStatusExtensions.Failure(errorMessage)
        };
}
