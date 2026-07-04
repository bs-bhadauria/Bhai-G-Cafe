namespace BhaiGCafe.Api.Models;

public sealed record CreateOrderRequest
{
    public CustomerDetailsRequest Customer { get; init; } = new();
    public List<OrderItemRequest> Items { get; init; } = [];
    public string PaymentMethod { get; init; } = "cod";
    public string Currency { get; init; } = "INR";
}

public sealed record CustomerDetailsRequest
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string DeliveryType { get; init; } = "dine";
    public string Address { get; init; } = string.Empty;
    public string TableNumber { get; init; } = string.Empty;
    public string SpecialInstructions { get; init; } = string.Empty;
}

public sealed record OrderItemRequest
{
    public string MenuItemId { get; init; } = string.Empty;
    public int Quantity { get; init; }
}

public sealed record OrderRecord
{
    public Guid Id { get; init; }
    public string PublicOrderId { get; init; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; init; }
    public CustomerDetails Customer { get; init; } = new();
    public List<OrderLine> Items { get; init; } = [];
    public PricingBreakdown Pricing { get; init; } = new();
    public string Currency { get; init; } = "INR";
    public string PaymentMethod { get; init; } = "cod";
    public string Status { get; init; } = "pending";
    public PaymentRecord Payment { get; init; } = new();
}

public sealed record CustomerDetails
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string DeliveryType { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string TableNumber { get; init; } = string.Empty;
    public string SpecialInstructions { get; init; } = string.Empty;
}

public sealed record OrderLine
{
    public string MenuItemId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal UnitPrice { get; init; }
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
}

public sealed record PricingBreakdown
{
    public decimal Subtotal { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal ServiceCharge { get; init; }
    public decimal DeliveryFee { get; init; }
    public decimal Total { get; init; }
}

public sealed record PaymentRecord
{
    public string Provider { get; init; } = string.Empty;
    public string Status { get; init; } = "pending";
    public string ProviderOrderId { get; init; } = string.Empty;
    public string ProviderPaymentId { get; init; } = string.Empty;
    public DateTimeOffset? PaidAtUtc { get; init; }
}

public sealed record CreateOrderResponse
{
    public string OrderId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public PricingBreakdown Pricing { get; init; } = new();
    public PaymentGatewayPayload? PaymentGateway { get; init; }
}

public sealed record PaymentGatewayPayload
{
    public string Provider { get; init; } = string.Empty;
    public string PublishableKey { get; init; } = string.Empty;
    public string ProviderOrderId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "INR";
    public string CustomerName { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
}

public sealed record AdminOrderStatusUpdateRequest
{
    public string Status { get; init; } = string.Empty;
}

public sealed record RazorpayCheckoutVerificationRequest
{
    public string PublicOrderId { get; init; } = string.Empty;
    public string ProviderOrderId { get; init; } = string.Empty;
    public string ProviderPaymentId { get; init; } = string.Empty;
    public string Signature { get; init; } = string.Empty;
}

public sealed record RazorpayWebhookRequest
{
    public string Event { get; init; } = string.Empty;
    public RazorpayPayload Payload { get; init; } = new();
}

public sealed record RazorpayPayload
{
    public RazorpayPaymentEntity Payment { get; init; } = new();
    public RazorpayOrderEntity Order { get; init; } = new();
}

public sealed record RazorpayPaymentEntity
{
    public RazorpayEntityDetails Entity { get; init; } = new();
}

public sealed record RazorpayOrderEntity
{
    public RazorpayEntityDetails Entity { get; init; } = new();
}

public sealed record RazorpayEntityDetails
{
    public string Id { get; init; } = string.Empty;
    public Dictionary<string, string>? Notes { get; init; }
}
