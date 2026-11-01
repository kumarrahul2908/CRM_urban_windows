using System.Text.Json.Serialization;

namespace Udc.Client.Models;

/// <summary>One rate row inside a <see cref="ProductGarment"/>'s <c>rates</c>, or an entry in
/// <see cref="ProductsResult"/>.UniversalServices (see src/services/catalog.ts rateOut()).</summary>
public sealed class ProductRate
{
    [JsonPropertyName("service_id")]
    public int ServiceId { get; set; }

    [JsonPropertyName("service_name")]
    public string ServiceName { get; set; } = "";

    [JsonPropertyName("billing_unit")]
    public string BillingUnit { get; set; } = "piece";

    [JsonPropertyName("piece_rate")]
    public decimal? PieceRate { get; set; }

    [JsonPropertyName("weight_rate")]
    public decimal? WeightRate { get; set; }

    [JsonPropertyName("tax_percent")]
    public decimal TaxPercent { get; set; }
}

public sealed class ProductGarment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("sku")]
    public string? Sku { get; set; }

    [JsonPropertyName("category_name")]
    public string? CategoryName { get; set; }

    [JsonPropertyName("default_service_id")]
    public int? DefaultServiceId { get; set; }

    [JsonPropertyName("rates")]
    public List<ProductRate> Rates { get; set; } = new();
}

public sealed class PriceListRef
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

/// <summary><c>GET /api/mobile/products</c> - the garment catalog with rates, for a resolved
/// price list, in one call.</summary>
public sealed class ProductsResult
{
    [JsonPropertyName("price_list")]
    public PriceListRef? PriceList { get; set; }

    [JsonPropertyName("garments")]
    public List<ProductGarment> Garments { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }
}

public sealed class UrgentLevel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("level_name")]
    public string LevelName { get; set; } = "";

    [JsonPropertyName("surcharge_percent")]
    public decimal SurchargePercent { get; set; }
}

public sealed class UrgentLevelsResult
{
    [JsonPropertyName("urgent_levels")]
    public List<UrgentLevel> UrgentLevels { get; set; } = new();
}

public sealed class StoreOption
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("store_name")]
    public string StoreName { get; set; } = "";
}

/// <summary><c>GET /api/mobile/pos_available_stores</c>.</summary>
public sealed class PosAvailableStoresResult
{
    [JsonPropertyName("stores")]
    public List<StoreOption> Stores { get; set; } = new();

    [JsonPropertyName("is_fixed")]
    public bool IsFixed { get; set; }
}

public sealed class PosCustomerInfo
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("home_delivery")]
    public bool HomeDelivery { get; set; }

    [JsonPropertyName("discount_percent")]
    public decimal DiscountPercent { get; set; }
}

/// <summary><c>GET /api/mobile/pos_customer_summary</c>.</summary>
public sealed class PosCustomerSummary
{
    [JsonPropertyName("customer")]
    public PosCustomerInfo Customer { get; set; } = new();

    [JsonPropertyName("price_list_name")]
    public string? PriceListName { get; set; }

    [JsonPropertyName("outstanding")]
    public decimal Outstanding { get; set; }

    [JsonPropertyName("wallet_balance")]
    public decimal WalletBalance { get; set; }
}

/// <summary>One line of <c>POST /api/mobile/pos_orders_create</c>'s <c>items</c> array (piece
/// mode only - see OrderService.priceLine()). Rate overrides and weight-mode lines inside a
/// piece order are not exposed by this app yet.</summary>
public sealed class CreateOrderItemLine
{
    [JsonPropertyName("garment_id")]
    public int GarmentId { get; set; }

    [JsonPropertyName("service_id")]
    public int ServiceId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }
}

/// <summary>Body for <c>POST /api/mobile/pos_orders_create</c> (piece-mode order creation).
/// Collecting payment at order-creation time isn't exposed here yet - use Record Payment right
/// after (see README's Known gaps).</summary>
public sealed class CreateOrderRequest
{
    [JsonPropertyName("client_request_id")]
    public string ClientRequestId { get; set; } = "";

    [JsonPropertyName("customer_id")]
    public int CustomerId { get; set; }

    [JsonPropertyName("store_id")]
    public int? StoreId { get; set; }

    [JsonPropertyName("items")]
    public List<CreateOrderItemLine> Items { get; set; } = new();

    [JsonPropertyName("urgent_level_id")]
    public int? UrgentLevelId { get; set; }

    [JsonPropertyName("discount_type")]
    public string DiscountType { get; set; } = "Percent";

    [JsonPropertyName("discount_value")]
    public decimal DiscountValue { get; set; }

    [JsonPropertyName("home_delivery")]
    public bool HomeDelivery { get; set; }

    [JsonPropertyName("delivery_charge")]
    public decimal DeliveryCharge { get; set; }

    [JsonPropertyName("notes")]
    public string Notes { get; set; } = "";

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }
}

public sealed class CreateOrderResult
{
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("invoice_number")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("grand_total")]
    public decimal GrandTotal { get; set; }

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();
}
