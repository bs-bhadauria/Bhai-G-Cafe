using System.Text;
using BhaiGCafe.Api.Models;
using BhaiGCafe.Api.Options;
using BhaiGCafe.Api.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables(prefix: "BHAIG_");
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.SingleLine = true;
    options.TimestampFormat = "HH:mm:ss ";
});

builder.Services.Configure<CorsOptions>(builder.Configuration.GetSection("Cors"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<DatabaseOptions>(builder.Configuration.GetSection("Database"));
builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection("Admin"));
builder.Services.Configure<PaymentsOptions>(builder.Configuration.GetSection("Payments"));
builder.Services.Configure<NotificationsOptions>(builder.Configuration.GetSection("Notifications"));

builder.Services.AddCors(options =>
{
    var corsOptions = builder.Configuration.GetSection("Cors").Get<CorsOptions>() ?? new CorsOptions();
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(corsOptions.AllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddHttpClient<NotificationService>();
builder.Services.AddHttpClient<RazorpayPaymentService>();
builder.Services.AddSingleton<MenuCatalogService>();
builder.Services.AddSingleton<FileOrderStore>();
builder.Services.AddSingleton<PostgresOrderStore>();
builder.Services.AddSingleton<IOrderStore>(serviceProvider =>
{
    var postgresOrderStore = serviceProvider.GetRequiredService<PostgresOrderStore>();
    return postgresOrderStore.IsConfigured
        ? postgresOrderStore
        : serviceProvider.GetRequiredService<FileOrderStore>();
});
builder.Services.AddSingleton<DatabaseBootstrapper>();
builder.Services.AddSingleton<PricingService>();
builder.Services.AddSingleton<OrderService>();

var app = builder.Build();

await app.Services.GetRequiredService<DatabaseBootstrapper>().InitializeAsync(CancellationToken.None);

app.UseCors();

app.MapGet("/api/health", (IOrderStore orderStore) => Results.Ok(new
{
    ok = true,
    service = "Bhai G Cafe API",
    utcTime = DateTimeOffset.UtcNow,
    storage = new
    {
        provider = orderStore.Provider,
        configured = orderStore.IsConfigured
    }
}));

app.MapGet("/api/menu", (MenuCatalogService menuCatalogService) =>
{
    return Results.Ok(menuCatalogService.GetAll());
});

app.MapGet("/api/config/public", (IOptions<PaymentsOptions> paymentOptions) =>
{
    var options = paymentOptions.Value;
    var notificationOptions = app.Services.GetRequiredService<IOptions<NotificationsOptions>>().Value;
    return Results.Ok(new
    {
        payment = new
        {
            provider = options.Provider,
            currency = options.Currency,
            onlineEnabled = !string.IsNullOrWhiteSpace(options.Razorpay.KeyId) && !string.IsNullOrWhiteSpace(options.Razorpay.KeySecret)
        },
        notifications = new
        {
            emailEnabled = notificationOptions.Email.Enabled && !string.IsNullOrWhiteSpace(notificationOptions.Email.ProviderUrl),
            smsEnabled = notificationOptions.Sms.Enabled && !string.IsNullOrWhiteSpace(notificationOptions.Sms.ProviderUrl)
        }
    });
});

app.MapPost("/api/orders", async (CreateOrderRequest request, OrderService orderService, CancellationToken cancellationToken) =>
{
    var (response, error) = await orderService.CreateAsync(request, cancellationToken);
    return error is null ? Results.Ok(response) : Results.BadRequest(new { error });
});

app.MapPost("/api/payments/razorpay/verify", async (RazorpayCheckoutVerificationRequest request, RazorpayPaymentService razorpayPaymentService, OrderService orderService, CancellationToken cancellationToken) =>
{
    if (!razorpayPaymentService.VerifyPaymentSignature(request.ProviderOrderId, request.ProviderPaymentId, request.Signature))
    {
        return Results.BadRequest(new { error = "Razorpay payment signature verification failed." });
    }

    var updated = await orderService.MarkPaidAsync(request.PublicOrderId, request.ProviderPaymentId, request.ProviderOrderId, cancellationToken);
    return updated
        ? Results.Ok(new { ok = true, orderId = request.PublicOrderId })
        : Results.NotFound(new { error = "Order not found or provider order mismatch." });
});

app.MapGet("/api/admin/orders", async (HttpContext context, OrderService orderService, IOptions<AdminOptions> adminOptions, CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(context, adminOptions.Value.ApiKey))
    {
        return Results.Unauthorized();
    }

    var orders = await orderService.GetAllAsync(cancellationToken);
    return Results.Ok(orders.OrderByDescending(order => order.CreatedAtUtc));
});

app.MapPatch("/api/admin/orders/{orderId}/status", async (string orderId, AdminOrderStatusUpdateRequest request, HttpContext context, OrderService orderService, IOptions<AdminOptions> adminOptions, CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(context, adminOptions.Value.ApiKey))
    {
        return Results.Unauthorized();
    }

    var updated = await orderService.UpdateStatusAsync(orderId, request.Status, cancellationToken);
    return updated ? Results.NoContent() : Results.NotFound();
});

app.MapGet("/api/admin/stats", async (HttpContext context, OrderService orderService, IOptions<AdminOptions> adminOptions, CancellationToken cancellationToken) =>
{
    if (!IsAuthorized(context, adminOptions.Value.ApiKey))
    {
        return Results.Unauthorized();
    }

    var orders = await orderService.GetAllAsync(cancellationToken);
    return Results.Ok(new
    {
        totalOrders = orders.Count,
        paidOrders = orders.Count(order => order.Payment.Status == "paid"),
        pendingOrders = orders.Count(order => order.Status is "pending" or "payment_pending"),
        revenue = orders.Where(order => order.Payment.Status == "paid" || order.Payment.Provider == "cod").Sum(order => order.Pricing.Total)
    });
});

app.MapPost("/api/payments/webhooks/razorpay", async (HttpRequest request, RazorpayPaymentService razorpayPaymentService, OrderService orderService, CancellationToken cancellationToken) =>
{
    using var reader = new StreamReader(request.Body, Encoding.UTF8);
    var rawBody = await reader.ReadToEndAsync(cancellationToken);
    var signature = request.Headers["X-Razorpay-Signature"].ToString();
    if (!razorpayPaymentService.VerifyWebhookSignature(rawBody, signature))
    {
        return Results.Unauthorized();
    }

    var payload = System.Text.Json.JsonSerializer.Deserialize<RazorpayWebhookRequest>(rawBody, new System.Text.Json.JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    var publicOrderId = payload?.Payload?.Order?.Entity?.Notes is { } notes && notes.TryGetValue("publicOrderId", out var orderId)
        ? orderId
        : string.Empty;

    if (string.IsNullOrWhiteSpace(publicOrderId))
    {
        return Results.BadRequest(new { error = "publicOrderId is missing from webhook notes." });
    }

    var paymentId = payload?.Payload?.Payment?.Entity?.Id ?? string.Empty;
    var providerOrderId = payload?.Payload?.Order?.Entity?.Id ?? string.Empty;
    var updated = await orderService.MarkPaidAsync(publicOrderId, paymentId, providerOrderId, cancellationToken);
    return updated ? Results.Ok(new { ok = true }) : Results.NotFound();
});

app.Run();

static bool IsAuthorized(HttpContext context, string configuredApiKey)
{
    if (string.IsNullOrWhiteSpace(configuredApiKey))
    {
        return false;
    }

    var providedApiKey = context.Request.Headers["X-Admin-Key"].ToString();
    return string.Equals(providedApiKey, configuredApiKey, StringComparison.Ordinal);
}
