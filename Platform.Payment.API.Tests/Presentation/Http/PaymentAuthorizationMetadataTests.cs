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
    public void SandboxActions_DoNotAllowAnonymous()
    {
        AssertDoesNotAllowAnonymous(nameof(PaymentsController.SandboxCheckout));
        AssertDoesNotAllowAnonymous(nameof(PaymentsController.SandboxComplete));
    }

    private static void AssertDoesNotAllowAnonymous(string methodName)
    {
        var method = typeof(PaymentsController).GetMethod(methodName);

        Assert.NotNull(method);
        Assert.Null(method!.GetCustomAttributes(typeof(AllowAnonymousAttribute), inherit: true).SingleOrDefault());
    }
}
