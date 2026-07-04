using BhaiGCafe.Api.Models;

namespace BhaiGCafe.Api.Services;

public sealed class PricingService
{
    public const decimal TaxRate = 0.08m;
    public const decimal ServiceCharge = 2m;
    public const decimal DeliveryFee = 5m;

    public PricingBreakdown BuildPricing(IEnumerable<OrderLine> items, string deliveryType)
    {
        var subtotal = items.Sum(item => item.LineTotal);
        var taxAmount = Math.Round(subtotal * TaxRate, 2, MidpointRounding.AwayFromZero);
        var deliveryFee = string.Equals(deliveryType, "delivery", StringComparison.OrdinalIgnoreCase) ? DeliveryFee : 0m;
        var total = subtotal + taxAmount + ServiceCharge + deliveryFee;

        return new PricingBreakdown
        {
            Subtotal = subtotal,
            TaxAmount = taxAmount,
            ServiceCharge = ServiceCharge,
            DeliveryFee = deliveryFee,
            Total = total
        };
    }
}
