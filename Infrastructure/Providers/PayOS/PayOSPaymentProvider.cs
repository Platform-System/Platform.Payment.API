using System.Text.Json;
using Microsoft.Extensions.Options;
using PayOS;
using PayOS.Models;
using PayOS.Models.V2.PaymentRequests;
using PayOS.Models.Webhooks;
using Platform.Contracts.Payments;
using Platform.Payment.API.Application.Abstractions.Providers;
using Platform.Payment.API.Application.Common.Models;
using Platform.Payment.API.Infrastructure.Configurations;

namespace Platform.Payment.API.Infrastructure.Providers.PayOS;

public sealed class PayOSPaymentProvider : IPaymentProvider
{
    private readonly PayOSClientOptions _payOSOptions;
    private readonly PaymentSettings _paymentSettings;

    public string Name => PaymentProviderNames.PayOS;

    public PayOSPaymentProvider(IOptions<PayOSClientOptions> payosOptions, IOptions<PaymentSettings> paymentSettings)
    {
        _payOSOptions = payosOptions.Value;
        _paymentSettings = paymentSettings.Value;
    }

    public async Task<PaymentLinkResponse> CreatePaymentLinkAsync(CreatePaymentRequest request, CancellationToken cancellationToken = default)
    {
        var payOSClient = CreateClient();
        var providerItems = request.Items.Select(x => new PaymentLinkItem
        {
            Name = x.Name,
            Quantity = x.Quantity,
            Price = x.Price
        }).ToList();

        var providerRequest = new CreatePaymentLinkRequest
        {
            OrderCode = request.ReferenceCode,
            Amount = request.Amount,
            Description = request.Description,
            ReturnUrl = _paymentSettings.ReturnUrl,
            CancelUrl = _paymentSettings.CancelUrl,
            Items = providerItems
        };

        var response = await payOSClient.PaymentRequests.CreateAsync(
            providerRequest,
            new RequestOptions<CreatePaymentLinkRequest>
            {
                CancellationToken = cancellationToken
            });

        return new PaymentLinkResponse
        {
            CheckoutUrl = response.CheckoutUrl,
            PaymentLinkId = response.PaymentLinkId,
            Amount = response.Amount,
            Currency = response.Currency,
            Status = response.Status.ToString()
        };
    }

    public async Task CancelPaymentLinkAsync(string? paymentLinkId, long referenceCode, CancellationToken cancellationToken = default)
    {
        var payOSClient = CreateClient();
        var idOrOrderCode = string.IsNullOrWhiteSpace(paymentLinkId)
            ? referenceCode.ToString()
            : paymentLinkId;

        await payOSClient.PaymentRequests.CancelAsync(
            idOrOrderCode,
            "Superseded by a newer payment link.",
            new RequestOptions<CancelPaymentLinkRequest>
            {
                CancellationToken = cancellationToken
            });
    }

    public async Task<PaymentWebhookResult?> VerifyWebhookAsync(string rawBody, CancellationToken cancellationToken = default)
    {
        var payOSClient = CreateClient();
        var webhook = JsonSerializer.Deserialize<Webhook>(rawBody);
        if (webhook is null)
            return null;

        var verified = await payOSClient.Webhooks.VerifyAsync(webhook);

        return new PaymentWebhookResult
        {
            PaymentLinkId = verified.PaymentLinkId,
            ReferenceCode = verified.OrderCode,
            Code = verified.Code,
            Signature = webhook.Signature
        };
    }

    private PayOSClient CreateClient()
    {
        EnsureConfigured();

        return new PayOSClient(
            _payOSOptions.ClientId,
            _payOSOptions.ApiKey,
            _payOSOptions.ChecksumKey);
    }

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_payOSOptions.ClientId)
            || string.IsNullOrWhiteSpace(_payOSOptions.ApiKey)
            || string.IsNullOrWhiteSpace(_payOSOptions.ChecksumKey))
        {
            throw new InvalidOperationException("PayOS provider is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_paymentSettings.ReturnUrl)
            || string.IsNullOrWhiteSpace(_paymentSettings.CancelUrl))
        {
            throw new InvalidOperationException("Payment return and cancel URLs are required for PayOS.");
        }
    }
}
