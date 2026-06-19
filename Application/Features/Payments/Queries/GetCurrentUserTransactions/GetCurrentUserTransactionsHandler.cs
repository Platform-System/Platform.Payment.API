using Platform.Application.Abstractions.Data;
using Platform.Application.Messaging;
using Platform.BuildingBlocks.Responses;
using Platform.Payment.API.Infrastructure.Persistence.Models;
using Platform.Payment.API.Application.Features.Payments.Mappers;
using Platform.Payment.API.Application.Features.Payments.Responses;
using Platform.SystemContext.Abstractions;
using Microsoft.AspNetCore.Http;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Platform.Payment.API.Application.Features.Payments.Queries.GetCurrentUserTransactions;

public sealed class GetCurrentUserTransactionsHandler
    : IQueryHandler<GetCurrentUserTransactionsQuery, PagedResult<PaymentTransactionResponse>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContext _userContext;

    public GetCurrentUserTransactionsHandler(IUnitOfWork unitOfWork, IUserContext userContext)
    {
        _unitOfWork = unitOfWork;
        _userContext = userContext;
    }

    public async Task<Result<PagedResult<PaymentTransactionResponse>>> Handle(
        GetCurrentUserTransactionsQuery query,
        CancellationToken cancellationToken)
    {
        if (_userContext.UserId is not Guid userId)
        {
            return Result<PagedResult<PaymentTransactionResponse>>.Failure(StatusCodes.Status401Unauthorized, "Unauthorized.");
        }

        var repository = _unitOfWork.GetRepository<PaymentTransactionModel>();

        var pagedTransactions = await repository.GetPagedAsync(
            query.Page,
            query.PageSize,
            x => x.UserId == userId,
            x => x.CreatedAt,
            isDescending: true,
            cancellationToken);

        var responses = new PagedResult<PaymentTransactionResponse>
        {
            Items = pagedTransactions.Items.Select(x => x.ToTransactionResponse()).ToList(),
            Page = pagedTransactions.Page,
            PageSize = pagedTransactions.PageSize,
            TotalCount = pagedTransactions.TotalCount
        };

        return Result<PagedResult<PaymentTransactionResponse>>.Success(responses);
    }
}
