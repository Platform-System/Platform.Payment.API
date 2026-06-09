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

    public static GetPaymentStatusResponse FailureGetPaymentStatus(string errorMessage)
        => new()
        {
            Status = ResponseStatusExtensions.Failure(errorMessage)
        };

    public static GetPaymentStatusResponse SuccessGetPaymentStatus()
        => new()
        {
            Status = ResponseStatusExtensions.Success()
        };
}
