using System.Text.RegularExpressions;
using BhaiGCafe.Api.Models;

namespace BhaiGCafe.Api.Services;

public sealed class OrderService
{
    private static readonly Regex EmailPattern = new(@"^[^\s@]+@[^\s@]+\.[^\s@]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex PhonePattern = new(@"^\+?[0-9][0-9\s-]{7,18}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private readonly MenuCatalogService _menuCatalogService;
    private readonly IOrderStore _orderStore;
    private readonly PricingService _pricingService;
    private readonly RazorpayPaymentService _paymentService;
    private readonly NotificationService _notificationService;

    public OrderService(
        MenuCatalogService menuCatalogService,
        IOrderStore orderStore,
        PricingService pricingService,
        RazorpayPaymentService paymentService,
        NotificationService notificationService)
    {
        _menuCatalogService = menuCatalogService;
        _orderStore = orderStore;
        _pricingService = pricingService;
        _paymentService = paymentService;
        _notificationService = notificationService;
    }

    public async Task<(CreateOrderResponse? Response, string? Error)> CreateAsync(CreateOrderRequest request, CancellationToken cancellationToken)
    {
        var validationError = Validate(request);
        if (validationError is not null)
        {
            return (null, validationError);
        }

        var orderLines = new List<OrderLine>();
        foreach (var item in request.Items)
        {
            if (!_menuCatalogService.TryGet(item.MenuItemId, out var menuItem))
            {
                return (null, $"Menu item not found: {item.MenuItemId}");
            }

            orderLines.Add(new OrderLine
            {
                MenuItemId = menuItem.Id,
                Name = menuItem.Name,
                UnitPrice = menuItem.Price,
                Quantity = item.Quantity,
                LineTotal = menuItem.Price * item.Quantity
            });
        }

        var pricing = _pricingService.BuildPricing(orderLines, request.Customer.DeliveryType);
        var publicOrderId = $"BG-{Guid.NewGuid():N}".Substring(0, 11).ToUpperInvariant();
        var order = new OrderRecord
        {
            Id = Guid.NewGuid(),
            PublicOrderId = publicOrderId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            Currency = string.IsNullOrWhiteSpace(request.Currency) ? "INR" : request.Currency.ToUpperInvariant(),
            PaymentMethod = request.PaymentMethod,
            Status = string.Equals(request.PaymentMethod, "cod", StringComparison.OrdinalIgnoreCase) ? "confirmed" : "payment_pending",
            Customer = new CustomerDetails
            {
                FullName = request.Customer.FullName.Trim(),
                Email = request.Customer.Email.Trim().ToLowerInvariant(),
                Phone = request.Customer.Phone.Trim(),
                DeliveryType = request.Customer.DeliveryType.Trim().ToLowerInvariant(),
                Address = request.Customer.Address.Trim(),
                TableNumber = request.Customer.TableNumber.Trim(),
                SpecialInstructions = request.Customer.SpecialInstructions.Trim()
            },
            Items = orderLines,
            Pricing = pricing,
            Payment = new PaymentRecord
            {
                Provider = string.Equals(request.PaymentMethod, "cod", StringComparison.OrdinalIgnoreCase) ? "cod" : "razorpay",
                Status = string.Equals(request.PaymentMethod, "cod", StringComparison.OrdinalIgnoreCase) ? "not_required" : "pending"
            }
        };

        var gatewayPayload = string.Equals(request.PaymentMethod, "cod", StringComparison.OrdinalIgnoreCase)
            ? null
            : await _paymentService.CreateOrderAsync(order, cancellationToken);

        if (!string.Equals(request.PaymentMethod, "cod", StringComparison.OrdinalIgnoreCase) && gatewayPayload is null)
        {
            return (null, "Online payment gateway could not be initialized. Verify Razorpay credentials and server network access, then try again.");
        }

        if (gatewayPayload is not null)
        {
            order = order with
            {
                Payment = new PaymentRecord
                {
                    Provider = gatewayPayload.Provider,
                    Status = "pending",
                    ProviderOrderId = gatewayPayload.ProviderOrderId
                }
            };
        }

        await _orderStore.SaveAsync(order, cancellationToken);
        await _notificationService.SendOrderCreatedAsync(order, cancellationToken);

        return (new CreateOrderResponse
        {
            OrderId = order.PublicOrderId,
            Status = order.Status,
            Pricing = order.Pricing,
            PaymentGateway = gatewayPayload
        }, null);
    }

    public Task<List<OrderRecord>> GetAllAsync(CancellationToken cancellationToken) => _orderStore.GetAllAsync(cancellationToken);

    public Task<OrderRecord?> FindAsync(string orderId, CancellationToken cancellationToken) => _orderStore.FindAsync(orderId, cancellationToken);

    public async Task<bool> UpdateStatusAsync(string orderId, string status, CancellationToken cancellationToken)
    {
        var existing = await _orderStore.FindAsync(orderId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        var updated = existing with { Status = status.Trim().ToLowerInvariant() };
        await _orderStore.SaveAsync(updated, cancellationToken);
        return true;
    }

    public async Task<bool> MarkPaidAsync(string orderId, string providerPaymentId, string? providerOrderId, CancellationToken cancellationToken)
    {
        var existing = await _orderStore.FindAsync(orderId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(existing.Payment.ProviderOrderId) &&
            !string.IsNullOrWhiteSpace(providerOrderId) &&
            !string.Equals(existing.Payment.ProviderOrderId, providerOrderId, StringComparison.Ordinal))
        {
            return false;
        }

        var updated = existing with
        {
            Status = "confirmed",
            Payment = new PaymentRecord
            {
                Provider = existing.Payment.Provider,
                ProviderOrderId = string.IsNullOrWhiteSpace(providerOrderId) ? existing.Payment.ProviderOrderId : providerOrderId,
                ProviderPaymentId = providerPaymentId,
                Status = "paid",
                PaidAtUtc = DateTimeOffset.UtcNow
            }
        };

        await _orderStore.SaveAsync(updated, cancellationToken);
        return true;
    }

    private static string? Validate(CreateOrderRequest request)
    {
        if (request.Items.Count == 0)
        {
            return "Cart is empty.";
        }

        if (string.IsNullOrWhiteSpace(request.Customer.FullName) || request.Customer.FullName.Trim().Length < 2)
        {
            return "Customer name is required.";
        }

        if (!EmailPattern.IsMatch(request.Customer.Email ?? string.Empty))
        {
            return "A valid email address is required.";
        }

        if (!PhonePattern.IsMatch(request.Customer.Phone ?? string.Empty))
        {
            return "A valid phone number is required.";
        }

        if (string.Equals(request.Customer.DeliveryType, "delivery", StringComparison.OrdinalIgnoreCase) &&
            string.IsNullOrWhiteSpace(request.Customer.Address))
        {
            return "Delivery address is required.";
        }

        if (request.Items.Any(item => item.Quantity <= 0 || item.Quantity > 20 || string.IsNullOrWhiteSpace(item.MenuItemId)))
        {
            return "Order items are invalid.";
        }

        return null;
    }
}
