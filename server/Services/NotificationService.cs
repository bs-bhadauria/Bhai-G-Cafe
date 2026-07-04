using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BhaiGCafe.Api.Models;
using BhaiGCafe.Api.Options;
using Microsoft.Extensions.Options;

namespace BhaiGCafe.Api.Services;

public sealed class NotificationService
{
    private readonly HttpClient _httpClient;
    private readonly NotificationsOptions _options;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(HttpClient httpClient, IOptions<NotificationsOptions> options, ILogger<NotificationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendOrderCreatedAsync(OrderRecord order, CancellationToken cancellationToken)
    {
        await SendEmailAsync(order, cancellationToken);
        await SendSmsAsync(order, cancellationToken);
    }

    private async Task SendEmailAsync(OrderRecord order, CancellationToken cancellationToken)
    {
        if (!_options.Email.Enabled || string.IsNullOrWhiteSpace(_options.Email.ProviderUrl))
        {
            _logger.LogInformation("Email notification skipped for order {OrderId}. Provider not configured.", order.PublicOrderId);
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Email.ProviderUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    from = _options.Email.FromAddress,
                    to = order.Customer.Email,
                    subject = $"Order confirmation - {order.PublicOrderId}",
                    body = $"Thank you {order.Customer.FullName}, your order total is {order.Pricing.Total} {order.Currency}."
                }), Encoding.UTF8, "application/json")
            };

            ApplyAuthHeader(request, _options.Email.AuthHeaderName, _options.Email.AuthScheme, _options.Email.ApiKey);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            _logger.LogInformation("Email provider returned {StatusCode} for order {OrderId}.", response.StatusCode, order.PublicOrderId);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Email notification failed for order {OrderId}.", order.PublicOrderId);
        }
    }

    private async Task SendSmsAsync(OrderRecord order, CancellationToken cancellationToken)
    {
        if (!_options.Sms.Enabled || string.IsNullOrWhiteSpace(_options.Sms.ProviderUrl))
        {
            _logger.LogInformation("SMS notification skipped for order {OrderId}. Provider not configured.", order.PublicOrderId);
            return;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _options.Sms.ProviderUrl)
            {
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    sender = _options.Sms.SenderId,
                    to = order.Customer.Phone,
                    message = $"Bhai G Cafe order {order.PublicOrderId} confirmed. Total: {order.Pricing.Total} {order.Currency}."
                }), Encoding.UTF8, "application/json")
            };

            ApplyAuthHeader(request, _options.Sms.AuthHeaderName, _options.Sms.AuthScheme, _options.Sms.ApiKey);
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            _logger.LogInformation("SMS provider returned {StatusCode} for order {OrderId}.", response.StatusCode, order.PublicOrderId);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "SMS notification failed for order {OrderId}.", order.PublicOrderId);
        }
    }

    private static void ApplyAuthHeader(HttpRequestMessage request, string? headerName, string? authScheme, string? apiKey)
    {
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(headerName))
        {
            return;
        }

        if (string.Equals(headerName, "Authorization", StringComparison.OrdinalIgnoreCase))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(string.IsNullOrWhiteSpace(authScheme) ? "Bearer" : authScheme.Trim(), apiKey);
            return;
        }

        request.Headers.TryAddWithoutValidation(headerName.Trim(), apiKey);
    }
}
