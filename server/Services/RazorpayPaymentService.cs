using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BhaiGCafe.Api.Models;
using BhaiGCafe.Api.Options;
using Microsoft.Extensions.Options;

namespace BhaiGCafe.Api.Services;

public sealed class RazorpayPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly PaymentsOptions _paymentsOptions;
    private readonly ILogger<RazorpayPaymentService> _logger;

    public RazorpayPaymentService(HttpClient httpClient, IOptions<PaymentsOptions> paymentsOptions, ILogger<RazorpayPaymentService> logger)
    {
        _httpClient = httpClient;
        _paymentsOptions = paymentsOptions.Value;
        _logger = logger;
    }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_paymentsOptions.Razorpay.KeyId) &&
        !string.IsNullOrWhiteSpace(_paymentsOptions.Razorpay.KeySecret);

    public async Task<PaymentGatewayPayload?> CreateOrderAsync(OrderRecord order, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
        {
            _logger.LogWarning("Razorpay is not configured. Skipping gateway order creation for {OrderId}.", order.PublicOrderId);
            return null;
        }

        var amountInPaise = (int)Math.Round(order.Pricing.Total * 100m, 0, MidpointRounding.AwayFromZero);
        var payload = new
        {
            amount = amountInPaise,
            currency = order.Currency,
            receipt = order.PublicOrderId,
            notes = new Dictionary<string, string>
            {
                ["publicOrderId"] = order.PublicOrderId
            }
        };

        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.razorpay.com/v1/orders")
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        var authBytes = Encoding.UTF8.GetBytes($"{_paymentsOptions.Razorpay.KeyId}:{_paymentsOptions.Razorpay.KeySecret}");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Razorpay order creation failed for {OrderId}: {StatusCode} {Body}", order.PublicOrderId, response.StatusCode, responseBody);
                return null;
            }

            using var document = JsonDocument.Parse(responseBody);
            var providerOrderId = document.RootElement.GetProperty("id").GetString() ?? string.Empty;

            return new PaymentGatewayPayload
            {
                Provider = "razorpay",
                PublishableKey = _paymentsOptions.Razorpay.KeyId,
                ProviderOrderId = providerOrderId,
                Amount = order.Pricing.Total,
                Currency = order.Currency,
                CustomerName = order.Customer.FullName,
                CustomerEmail = order.Customer.Email,
                CustomerPhone = order.Customer.Phone
            };
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(exception, "Razorpay HTTP request failed for {OrderId}.", order.PublicOrderId);
            return null;
        }
    }

    public bool VerifyWebhookSignature(string requestBody, string? signature)
    {
        if (string.IsNullOrWhiteSpace(_paymentsOptions.Razorpay.WebhookSecret) || string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_paymentsOptions.Razorpay.WebhookSecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(requestBody));
        var expectedSignature = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant()));
    }

    public bool VerifyPaymentSignature(string providerOrderId, string providerPaymentId, string? signature)
    {
        if (string.IsNullOrWhiteSpace(_paymentsOptions.Razorpay.KeySecret) ||
            string.IsNullOrWhiteSpace(providerOrderId) ||
            string.IsNullOrWhiteSpace(providerPaymentId) ||
            string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        var payload = $"{providerOrderId.Trim()}|{providerPaymentId.Trim()}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_paymentsOptions.Razorpay.KeySecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var expectedSignature = Convert.ToHexString(hashBytes).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedSignature),
            Encoding.UTF8.GetBytes(signature.Trim().ToLowerInvariant()));
    }
}
