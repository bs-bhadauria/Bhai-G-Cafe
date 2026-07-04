namespace BhaiGCafe.Api.Options;

public sealed class StorageOptions
{
    public string DataDirectory { get; init; } = "App_Data";
    public string OrdersFile { get; init; } = "orders.json";
}

public sealed class DatabaseOptions
{
    public string Provider { get; init; } = "postgres";
    public bool AutoMigrateOnStartup { get; init; } = true;
    public bool ImportLegacyJsonOnStartup { get; init; }
}

public sealed class CorsOptions
{
    public string[] AllowedOrigins { get; init; } = [];
}

public sealed class AdminOptions
{
    public string ApiKey { get; init; } = string.Empty;
}

public sealed class PaymentsOptions
{
    public string Provider { get; init; } = "Razorpay";
    public string Currency { get; init; } = "INR";
    public RazorpayOptions Razorpay { get; init; } = new();
}

public sealed class RazorpayOptions
{
    public string KeyId { get; init; } = string.Empty;
    public string KeySecret { get; init; } = string.Empty;
    public string WebhookSecret { get; init; } = string.Empty;
}

public sealed class NotificationsOptions
{
    public EmailOptions Email { get; init; } = new();
    public SmsOptions Sms { get; init; } = new();
}

public sealed class EmailOptions
{
    public bool Enabled { get; init; }
    public string ProviderUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string AuthHeaderName { get; init; } = "Authorization";
    public string AuthScheme { get; init; } = "Bearer";
    public string FromAddress { get; init; } = string.Empty;
}

public sealed class SmsOptions
{
    public bool Enabled { get; init; }
    public string ProviderUrl { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string AuthHeaderName { get; init; } = "Authorization";
    public string AuthScheme { get; init; } = "Bearer";
    public string SenderId { get; init; } = string.Empty;
}
