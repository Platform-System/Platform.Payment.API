using System.Collections;
using System.Linq.Expressions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Platform.Application.Abstractions.Data;
using Platform.BuildingBlocks.Responses;
using Platform.Contracts.Payments;
using Platform.Domain.Common;
using Platform.Payment.API.Application.Abstractions.Providers;
using Platform.Payment.API.Application.Common.Models;
using Platform.Payment.API.Application.Features.Payments.Commands.Create;
using Platform.Payment.API.Domain.Enums;
using Platform.Payment.API.Infrastructure.Persistence.Models;
using Xunit;

namespace Platform.Payment.API.Tests.Application.Features.Payments.Commands.Create;

public sealed class CreatePaymentHandlerTests
{
    [Fact]
    public async Task Handle_WhenExistingPendingPaymentHasCheckoutUrl_ReturnsExistingLinkWithoutCallingProvider()
    {
        var request = CreateRequest();
        var existingPayment = CreatePaymentModel(
            request,
            provider: PaymentProviderNames.PayOS,
            checkoutUrl: "https://pay.local/existing",
            paymentLinkId: "plink-existing",
            status: PaymentStatus.Pending);
        var repository = new FakeRepository<PaymentTransactionModel>(existingPayment);
        var unitOfWork = new FakeUnitOfWork(repository);
        var provider = new FakePaymentProvider(PaymentProviderNames.PayOS);
        var handler = new CreatePaymentHandler(unitOfWork, [provider]);

        var result = await handler.Handle(new CreatePaymentCommand(request), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://pay.local/existing", result.Value.CheckoutUrl);
        Assert.Equal(0, provider.CreateCallCount);
        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task Handle_WhenCancellingStalePaymentFails_Returns502AndSkipsNewProviderCall()
    {
        var request = CreateRequest();
        var stalePayment = CreatePaymentModel(
            request,
            provider: PaymentProviderNames.Sandbox,
            checkoutUrl: "https://sandbox.local/checkout",
            paymentLinkId: "sandbox-link",
            status: PaymentStatus.Pending);
        var repository = new FakeRepository<PaymentTransactionModel>(stalePayment);
        var unitOfWork = new FakeUnitOfWork(repository);
        var staleProvider = new FakePaymentProvider(PaymentProviderNames.Sandbox)
        {
            CancelException = new InvalidOperationException("cancel failed")
        };
        var provider = new FakePaymentProvider(PaymentProviderNames.PayOS);
        var handler = new CreatePaymentHandler(unitOfWork, [provider, staleProvider]);

        var result = await handler.Handle(new CreatePaymentCommand(request), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status502BadGateway, result.StatusCode);
        Assert.Equal("Unable to cancel the existing Sandbox payment link.", Assert.Single(result.Errors));
        Assert.Equal(1, staleProvider.CancelCallCount);
        Assert.Equal(0, provider.CreateCallCount);
        Assert.Equal(PaymentStatus.Pending, stalePayment.Status);
    }

    [Fact]
    public async Task Handle_WhenProviderReturnsIncompleteLink_Returns502AndDoesNotAddPayment()
    {
        var request = CreateRequest();
        var repository = new FakeRepository<PaymentTransactionModel>();
        var unitOfWork = new FakeUnitOfWork(repository);
        var provider = new FakePaymentProvider(PaymentProviderNames.PayOS)
        {
            CreateResponse = new PaymentLinkResponse
            {
                PaymentLinkId = string.Empty,
                CheckoutUrl = string.Empty,
                Amount = request.Amount,
                Currency = string.Empty
            }
        };
        var handler = new CreatePaymentHandler(unitOfWork, [provider]);

        var result = await handler.Handle(new CreatePaymentCommand(request), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status502BadGateway, result.StatusCode);
        Assert.Equal("Unable to create a payment link.", Assert.Single(result.Errors));
        Assert.Equal(1, provider.CreateCallCount);
        Assert.Equal(0, repository.AddCallCount);
    }

    [Fact]
    public async Task Handle_WhenStalePaymentIsCancelledAndProviderSucceeds_AddsNewPayment()
    {
        var request = CreateRequest();
        var stalePayment = CreatePaymentModel(
            request,
            provider: PaymentProviderNames.Sandbox,
            checkoutUrl: "https://sandbox.local/checkout",
            paymentLinkId: "sandbox-link",
            status: PaymentStatus.Pending);
        var repository = new FakeRepository<PaymentTransactionModel>(stalePayment);
        var unitOfWork = new FakeUnitOfWork(repository);
        var staleProvider = new FakePaymentProvider(PaymentProviderNames.Sandbox);
        var provider = new FakePaymentProvider(PaymentProviderNames.PayOS)
        {
            CreateResponse = new PaymentLinkResponse
            {
                PaymentLinkId = "payos-link",
                CheckoutUrl = "https://pay.local/new",
                Amount = request.Amount,
                Currency = request.Currency,
                Status = "Pending"
            }
        };
        var handler = new CreatePaymentHandler(unitOfWork, [provider, staleProvider]);

        var result = await handler.Handle(new CreatePaymentCommand(request), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://pay.local/new", result.Value.CheckoutUrl);
        Assert.Equal(1, staleProvider.CancelCallCount);
        Assert.Equal(1, provider.CreateCallCount);
        Assert.Equal(PaymentStatus.Cancelled, stalePayment.Status);
        Assert.Equal(1, repository.AddCallCount);
        Assert.Equal(2, repository.Count);
    }

    private static CreatePaymentRequest CreateRequest()
    {
        return new CreatePaymentRequest
        {
            ReferenceType = PaymentReferenceType.Order,
            ReferenceId = Guid.NewGuid(),
            ReferenceCode = 123456,
            Provider = PaymentProviderNames.PayOS,
            Amount = 100_000,
            Currency = "VND",
            Description = "Order 123456",
            Items = []
        };
    }

    private static PaymentTransactionModel CreatePaymentModel(
        CreatePaymentRequest request,
        string provider,
        string checkoutUrl,
        string paymentLinkId,
        PaymentStatus status)
    {
        return new PaymentTransactionModel
        {
            Id = Guid.NewGuid(),
            ReferenceType = request.ReferenceType,
            ReferenceId = request.ReferenceId,
            ReferenceCode = request.ReferenceCode,
            Provider = provider,
            CheckoutUrl = checkoutUrl,
            PaymentLinkId = paymentLinkId,
            Amount = request.Amount,
            Currency = request.Currency,
            Status = status
        };
    }

    private sealed class FakePaymentProvider : IPaymentProvider
    {
        public FakePaymentProvider(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public int CreateCallCount { get; private set; }
        public int CancelCallCount { get; private set; }
        public Exception? CancelException { get; init; }
        public PaymentLinkResponse CreateResponse { get; init; } = new()
        {
            PaymentLinkId = "default-link",
            CheckoutUrl = "https://pay.local/default",
            Amount = 100_000,
            Currency = "VND",
            Status = "Pending"
        };

        public Task<PaymentLinkResponse> CreatePaymentLinkAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
        {
            CreateCallCount++;
            return Task.FromResult(CreateResponse);
        }

        public Task CancelPaymentLinkAsync(string? paymentLinkId, long referenceCode, CancellationToken cancellationToken = default)
        {
            CancelCallCount++;

            if (CancelException is not null)
            {
                throw CancelException;
            }

            return Task.CompletedTask;
        }

        public Task<PaymentWebhookResult?> VerifyWebhookAsync(string rawBody, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly IGenericRepository<PaymentTransactionModel> _repository;

        public FakeUnitOfWork(IGenericRepository<PaymentTransactionModel> repository)
        {
            _repository = repository;
        }

        public bool HasActiveTransaction => false;

        public Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IGenericRepository<T> GetRepository<T>() where T : Entity
        {
            if (typeof(T) == typeof(PaymentTransactionModel))
            {
                return (IGenericRepository<T>)_repository;
            }

            throw new NotSupportedException();
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeRepository<T> : IGenericRepository<T> where T : Entity
    {
        private readonly List<T> _entities = [];

        public FakeRepository(params T[] entities)
        {
            _entities.AddRange(entities);
        }

        public int AddCallCount { get; private set; }
        public int Count => _entities.Count;

        public IQueryable<T> GetQueryable()
        {
            return new TestAsyncEnumerable<T>(_entities);
        }

        public Task AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            AddCallCount++;
            _entities.Add(entity);
            return Task.CompletedTask;
        }

        public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes) => throw new NotSupportedException();
        public Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes) => throw new NotSupportedException();
        public Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes) => throw new NotSupportedException();
        public Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? filter = null, Expression<Func<T, object>>? orderBy = null, bool isDescending = false, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes) => throw new NotSupportedException();
        public Task<T?> FindAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes) => throw new NotSupportedException();
        public void Update(T entity) => throw new NotSupportedException();
        public void Remove(T entity) => throw new NotSupportedException();
        public Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate) => throw new NotSupportedException();
        public Task<int> DeleteInBatchesAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, DateTime>> orderBy, Expression<Func<T, Guid>> keySelector, int batchSize = 100) => throw new NotSupportedException();
        public Task<int> TotalAsync(Expression<Func<T, bool>> predicate) => throw new NotSupportedException();
    }

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable)
            : base(enumerable)
        {
        }

        public TestAsyncEnumerable(Expression expression)
            : base(expression)
        {
        }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
        {
            return new TestAsyncEnumerator<T>(((IEnumerable<T>)this).GetEnumerator());
        }

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    private sealed class TestAsyncEnumerator<T> : IAsyncEnumerator<T>
    {
        private readonly IEnumerator<T> _inner;

        public TestAsyncEnumerator(IEnumerator<T> inner)
        {
            _inner = inner;
        }

        public T Current => _inner.Current;

        public ValueTask DisposeAsync()
        {
            _inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync()
        {
            return ValueTask.FromResult(_inner.MoveNext());
        }
    }

    private sealed class TestAsyncQueryProvider<TEntity> : IAsyncQueryProvider
    {
        private readonly IQueryProvider _inner;

        public TestAsyncQueryProvider(IQueryProvider inner)
        {
            _inner = inner;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            var elementType = expression.Type.GetGenericArguments().First();
            var asyncEnumerableType = typeof(TestAsyncEnumerable<>).MakeGenericType(elementType);
            return (IQueryable)Activator.CreateInstance(asyncEnumerableType, expression)!;
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new TestAsyncEnumerable<TElement>(expression);
        }

        public object? Execute(Expression expression)
        {
            return _inner.Execute(expression);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return _inner.Execute<TResult>(expression);
        }

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken = default)
        {
            return Execute<TResult>(expression);
        }
    }
}
