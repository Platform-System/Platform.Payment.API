using System.Linq.Expressions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Platform.Application.Abstractions.Data;
using Platform.BuildingBlocks.Responses;
using Platform.Contracts.Payments;
using Platform.Contracts.Messages.Payments;
using Platform.Domain.Common;
using Platform.Payment.API.Application.Abstractions.Messaging;
using Platform.Payment.API.Application.Abstractions.Providers;
using Platform.Payment.API.Application.Common.Models;
using Platform.Payment.API.Application.Features.Payments.Commands.ProcessWebhook;
using Platform.Payment.API.Domain.Enums;
using Platform.Payment.API.Infrastructure.Persistence.Models;
using Xunit;

namespace Platform.Payment.API.Tests.Application.Features.Payments.Commands.ProcessWebhook;

public sealed class ProcessPaymentWebhookHandlerTests
{
    [Fact]
    public async Task Handle_WhenProviderIsNotSupported_Returns400()
    {
        var handler = new ProcessPaymentWebhookHandler(
            new FakeUnitOfWork(new FakeRepository<PaymentTransactionModel>()),
            [],
            new FakePaymentOutboxWriter());

        var result = await handler.Handle(
            new ProcessPaymentWebhookCommand("UnknownProvider", "{}"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        Assert.Equal("Payment provider is not supported.", Assert.Single(result.Errors));
    }

    [Fact]
    public async Task Handle_WhenWebhookCannotBeVerified_ReturnsSuccessWithoutMutation()
    {
        var repository = new FakeRepository<PaymentTransactionModel>();
        var handler = new ProcessPaymentWebhookHandler(
            new FakeUnitOfWork(repository),
            [new FakePaymentProvider("PayOS")],
            new FakePaymentOutboxWriter());

        var command = new ProcessPaymentWebhookCommand("PayOS", "{invalid}");
        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(0, repository.FindCallCount);
    }

    [Fact]
    public async Task Handle_WhenPendingPaymentIsNotFound_ReturnsSuccessWithoutEvent()
    {
        var provider = new FakePaymentProvider("PayOS")
        {
            WebhookResult = new PaymentWebhookResult
            {
                PaymentLinkId = "plink-404",
                ReferenceCode = 123456,
                Code = "00"
            }
        };
        var outboxWriter = new FakePaymentOutboxWriter();
        var repository = new FakeRepository<PaymentTransactionModel>();
        var handler = new ProcessPaymentWebhookHandler(new FakeUnitOfWork(repository), [provider], outboxWriter);
        var command = new ProcessPaymentWebhookCommand("PayOS", "{}");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(outboxWriter.SucceededMessages);
        Assert.Empty(outboxWriter.CancelledMessages);
        Assert.Equal(2, repository.FindCallCount);
    }

    [Fact]
    public async Task Handle_WhenWebhookCodeIsSuccess_MarksPaymentPaidAndEnqueuesSucceededMessage()
    {
        var payment = CreatePayment(
            provider: "PayOS",
            paymentLinkId: "plink-1",
            status: PaymentStatus.Pending);
        var provider = new FakePaymentProvider("PayOS")
        {
            WebhookResult = new PaymentWebhookResult
            {
                PaymentLinkId = "plink-1",
                ReferenceCode = payment.ReferenceCode,
                Code = "00"
            }
        };
        var outboxWriter = new FakePaymentOutboxWriter();
        var repository = new FakeRepository<PaymentTransactionModel>(payment);
        var handler = new ProcessPaymentWebhookHandler(new FakeUnitOfWork(repository), [provider], outboxWriter);
        var command = new ProcessPaymentWebhookCommand("PayOS", "{}");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Paid, payment.Status);
        var message = Assert.Single(outboxWriter.SucceededMessages);
        Assert.NotEqual(Guid.Empty, message.MessageId);
        Assert.Equal(payment.Id, message.PaymentId);
        Assert.Equal(payment.ReferenceCode, message.ReferenceCode);
    }

    [Fact]
    public async Task Handle_WhenWebhookCodeIsFailure_MarksPaymentCancelledAndEnqueuesCancelledMessage()
    {
        var payment = CreatePayment(
            provider: "PayOS",
            paymentLinkId: "plink-2",
            status: PaymentStatus.Pending);
        var provider = new FakePaymentProvider("PayOS")
        {
            WebhookResult = new PaymentWebhookResult
            {
                PaymentLinkId = "plink-2",
                ReferenceCode = payment.ReferenceCode,
                Code = "CANCELLED"
            }
        };
        var outboxWriter = new FakePaymentOutboxWriter();
        var repository = new FakeRepository<PaymentTransactionModel>(payment);
        var handler = new ProcessPaymentWebhookHandler(new FakeUnitOfWork(repository), [provider], outboxWriter);
        var command = new ProcessPaymentWebhookCommand("PayOS", "{}");

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(PaymentStatus.Cancelled, payment.Status);
        var message = Assert.Single(outboxWriter.CancelledMessages);
        Assert.NotEqual(Guid.Empty, message.MessageId);
        Assert.Equal(payment.Id, message.PaymentId);
        Assert.Equal("CANCELLED", message.ReasonCode);
    }

    private static PaymentTransactionModel CreatePayment(string provider, string paymentLinkId, PaymentStatus status)
    {
        return new PaymentTransactionModel
        {
            Id = Guid.NewGuid(),
            ReferenceType = "Order",
            ReferenceId = Guid.NewGuid(),
            ReferenceCode = 123456,
            Provider = provider,
            PaymentLinkId = paymentLinkId,
            CheckoutUrl = "https://pay.local/checkout",
            Amount = 100_000,
            Currency = "VND",
            Status = status,
            UserId = Guid.NewGuid()
        };
    }

    private sealed class FakePaymentProvider : IPaymentProvider
    {
        public FakePaymentProvider(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public PaymentWebhookResult? WebhookResult { get; init; }

        public Task<PaymentLinkResponse> CreatePaymentLinkAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task CancelPaymentLinkAsync(string? paymentLinkId, long referenceCode, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<PaymentWebhookResult?> VerifyWebhookAsync(string rawBody, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(WebhookResult);
        }
    }

    private sealed class FakePaymentOutboxWriter : IPaymentOutboxWriter
    {
        public List<PaymentSucceeded> SucceededMessages { get; } = [];
        public List<PaymentCancelled> CancelledMessages { get; } = [];

        public Task EnqueueAsync(PaymentSucceeded message, CancellationToken cancellationToken = default)
        {
            SucceededMessages.Add(message);
            return Task.CompletedTask;
        }

        public Task EnqueueAsync(PaymentCancelled message, CancellationToken cancellationToken = default)
        {
            CancelledMessages.Add(message);
            return Task.CompletedTask;
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

        public int FindCallCount { get; private set; }

        public Task<T?> FindAsync(Expression<Func<T, bool>> predicate, bool asNoTracking = true, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
        {
            FindCallCount++;
            var compiled = predicate.Compile();
            return Task.FromResult(_entities.FirstOrDefault(compiled));
        }

        public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes) => throw new NotSupportedException();
        public Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes) => throw new NotSupportedException();
        public Task<List<T>> GetAllAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes) => throw new NotSupportedException();
        public Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, Expression<Func<T, bool>>? filter = null, Expression<Func<T, object>>? orderBy = null, bool isDescending = false, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes) => throw new NotSupportedException();
        public Task AddAsync(T entity, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Update(T entity) => throw new NotSupportedException();
        public void Remove(T entity) => throw new NotSupportedException();
        public Task<int> DeleteRangeAsync(Expression<Func<T, bool>> predicate) => throw new NotSupportedException();
        public Task<int> DeleteInBatchesAsync(Expression<Func<T, bool>> predicate, Expression<Func<T, DateTime>> orderBy, Expression<Func<T, Guid>> keySelector, int batchSize = 100) => throw new NotSupportedException();
        public Task<int> TotalAsync(Expression<Func<T, bool>> predicate) => throw new NotSupportedException();
    }
}
