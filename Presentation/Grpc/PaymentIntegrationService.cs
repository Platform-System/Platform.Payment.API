using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using MediatR;
using Platform.Payment.API.Application.Features.Payments.Commands.Create;
using Platform.Payment.API.Infrastructure.Data;
using Platform.Payment.Grpc;

namespace Platform.Payment.API.Presentation.Grpc;

public sealed class PaymentIntegrationService : PaymentIntegration.PaymentIntegrationBase
{
    private readonly ISender _sender;
    private readonly PaymentDbContext _dbContext;

    public PaymentIntegrationService(ISender sender, PaymentDbContext dbContext)
    {
        _sender = sender;
        _dbContext = dbContext;
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

    public override async Task<GetPaymentStatusResponse> GetPaymentStatus(
        GetPaymentStatusRequest request,
        ServerCallContext context)
    {
        if (!Guid.TryParse(request.ReferenceId, out var referenceId))
            return PaymentIntegrationResponses.FailureGetPaymentStatus("Invalid reference id.");

        var payment = await _dbContext.Payments
            .AsNoTracking()
            .Where(x =>
                x.ReferenceType == request.ReferenceType
                && x.ReferenceId == referenceId
                && x.ReferenceCode == request.ReferenceCode)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync(context.CancellationToken);

        return payment is null
            ? PaymentIntegrationResponses.SuccessGetPaymentStatus()
            : payment.ToStatusResponse();
    }
}
