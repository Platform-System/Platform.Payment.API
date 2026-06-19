using Platform.Application.Messaging;
using Platform.BuildingBlocks.Requests;
using Platform.BuildingBlocks.Responses;
using Platform.Payment.API.Application.Features.Payments.Responses;

namespace Platform.Payment.API.Application.Features.Payments.Queries.GetCurrentUserTransactions;

public sealed class GetCurrentUserTransactionsQuery : PagingRequest, IQuery<PagedResult<PaymentTransactionResponse>>
{
}
