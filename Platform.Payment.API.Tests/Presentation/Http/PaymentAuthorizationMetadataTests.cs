using Microsoft.AspNetCore.Authorization;
using Platform.Payment.API.Presentation.Http;
using Xunit;

namespace Platform.Payment.API.Tests.Presentation.Http;

public sealed class PaymentAuthorizationMetadataTests
{
    [Fact]
    public void Controller_DefaultsToAuthorize()
    {
        Assert.NotNull(typeof(PaymentsController)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .SingleOrDefault());
    }

    [Fact]
    public void WebhookAction_AllowsAnonymous()
    {
        var method = typeof(PaymentsController).GetMethod(nameof(PaymentsController.HandleWebhook));

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).SingleOrDefault());
    }

    [Fact]
    public void SandboxActions_AllowAnonymous()
    {
        AssertAllowsAnonymous(nameof(PaymentsController.SandboxCheckout));
        AssertAllowsAnonymous(nameof(PaymentsController.SandboxComplete));
    }

    private static void AssertAllowsAnonymous(string methodName)
    {
        var method = typeof(PaymentsController).GetMethod(methodName);

        Assert.NotNull(method);
        Assert.NotNull(method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).SingleOrDefault());
    }
}
