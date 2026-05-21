using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Platform.BuildingBlocks.Responses;
using Platform.Payment.API.Infrastructure.Configurations;
using Platform.Payment.API.Infrastructure.Providers.Sandbox;
using Platform.Payment.API.Presentation.Http;
using Xunit;

namespace Platform.Payment.API.Tests.Presentation.Http;

public sealed class PaymentsControllerTests
{
    [Fact]
    public void SandboxCheckout_WhenSandboxDisabled_ReturnsNotFound()
    {
        var controller = CreateController(
            new SandboxPaymentOptions
            {
                Enabled = false,
                PublicBaseUrl = "http://localhost:8080"
            },
            environmentName: "Development");

        var result = controller.SandboxCheckout(123456, "sandbox-link");

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public void SandboxCheckout_WhenNotDevelopment_ReturnsNotFound()
    {
        var controller = CreateController(
            new SandboxPaymentOptions
            {
                Enabled = true,
                PublicBaseUrl = "http://localhost:8080"
            },
            environmentName: "Production");

        var result = controller.SandboxCheckout(123456, "sandbox-link");

        Assert.IsType<NotFoundResult>(result);
    }

    private static PaymentsController CreateController(SandboxPaymentOptions options, string environmentName)
    {
        return new PaymentsController(
            new FakeSender(),
            Options.Create(options),
            new SandboxCheckoutPageRenderer(),
            new FakeWebHostEnvironment(environmentName));
    }

    private sealed class FakeSender : ISender
    {
        public Task<TResponse> Send<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            if (typeof(TResponse) == typeof(Result<Unit>))
            {
                return Task.FromResult((TResponse)(object)Result<Unit>.Success(Unit.Value));
            }

            throw new NotSupportedException();
        }

        public Task Send<TRequest>(TRequest request, CancellationToken cancellationToken = default)
            where TRequest : IRequest
        {
            return Task.CompletedTask;
        }

        public Task<object?> Send(object request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<TResponse> CreateStream<TResponse>(IStreamRequest<TResponse> request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public IAsyncEnumerable<object?> CreateStream(object request, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public FakeWebHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Platform.Payment.API.Tests";
        public string WebRootPath { get; set; } = string.Empty;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = string.Empty;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
