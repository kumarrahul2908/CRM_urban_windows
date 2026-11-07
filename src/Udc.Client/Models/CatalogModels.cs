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

public sealed class BrandOption
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("brand_name")]
    public string BrandName { get; set; } = "";
}

/// <summary><c>GET /api/mobile/pos_brands</c>.</summary>
public sealed class BrandsResult
{
    [JsonPropertyName("brands")]
    public List<BrandOption> Brands { get; set; } = new();
}

public sealed class ColorOption
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("color_name")]
    public string ColorName { get; set; } = "";
}

/// <summary><c>GET /api/mobile/pos_colors</c>.</summary>
public sealed class ColorsResult
{
    [JsonPropertyName("colors")]
    public List<ColorOption> Colors { get; set; } = new();
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

    /// <summary>Weight-priced, garment-independent services (e.g. "Wash &amp; Fold") already
    /// resolved for this price list - the rate picker for Per-Weight (Laundry) order creation.
    /// Only entries with a configured <see cref="ProductRate.WeightRate"/> are included (see
    /// src/services/catalog.ts listProductsWithRates()).</summary>
    [JsonPropertyName("universal_services")]
    public List<ProductRate> UniversalServices { get; set; } = new();

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
/// mode only - see OrderService.priceLine()). Weight-mode lines inside a piece order (a garment
/// priced by weight rather than per piece) are not exposed by this app - use a Laundry order for
/// weight-based pricing instead.</summary>
public sealed class CreateOrderItemLine
{
    [JsonPropertyName("garment_id")]
    public int GarmentId { get; set; }

    [JsonPropertyName("service_id")]
    public int ServiceId { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("brand_id")]
    public int? BrandId { get; set; }

    [JsonPropertyName("color_id")]
    public int? ColorId { get; set; }

    [JsonPropertyName("remarks")]
    public string Remarks { get; set; } = "";

    /// <summary>Must be true together with a non-null <see cref="RateOverride"/> for the override
    /// to go through <c>pos.manual_rate_override</c>'s permission check and be audit-logged - see
    /// README's Known gaps for a backend quirk where <see cref="RateOverride"/> alone (without
    /// this flag) is silently honored too, with no permission check at all. This app always sends
    /// both together or neither, so it never exercises that path.</summary>
    [JsonPropertyName("manual_override")]
    public bool ManualOverride { get; set; }

    [JsonPropertyName("rate_override")]
    public decimal? RateOverride { get; set; }
}

/// <summary>Body for <c>POST /api/mobile/pos_orders_create</c> (piece-mode order creation).</summary>
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

    [JsonPropertyName("payments")]
    public List<PaymentLine> Payments { get; set; } = new();

    [JsonPropertyName("wallet_amount")]
    public decimal WalletAmount { get; set; }
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

// ---- Per-Weight (Laundry) order creation --------------------------------------------------

/// <summary>One garment being tagged, inside a <see cref="CreateLaundryBatchLine"/>.</summary>
public sealed class LaundryBatchGarmentLine
{
    [JsonPropertyName("garment_id")]
    public int GarmentId { get; set; }

    [JsonPropertyName("quantity")]
    public int Quantity { get; set; }
}

/// <summary>One line of <c>POST /api/mobile/laundry_orders_create</c>'s <c>batches</c> array -
/// one weight-priced service (e.g. "Wash &amp; Fold") plus the individual garments tagged into
/// it. <see cref="GarmentCount"/> must equal the sum of <see cref="Garments"/>' quantities (see
/// OrderService.createLaundryOrder()) - the app always computes it that way, so a mismatch can't
/// happen from this client. Unlike pos_orders_create, this endpoint does not re-resolve
/// <see cref="Rate"/> server-side - it trusts whatever the client sends, so the app always fills
/// it from the price list's own resolved rate (<c>products</c>' <c>universal_services</c>) rather
/// than letting it be typed in.</summary>
public sealed class CreateLaundryBatchLine
{
    [JsonPropertyName("service_id")]
    public int ServiceId { get; set; }

    [JsonPropertyName("weight")]
    public decimal Weight { get; set; }

    [JsonPropertyName("rate")]
    public decimal Rate { get; set; }

    [JsonPropertyName("garment_count")]
    public int GarmentCount { get; set; }

    [JsonPropertyName("garments")]
    public List<LaundryBatchGarmentLine> Garments { get; set; } = new();
}

/// <summary>Body for <c>POST /api/mobile/laundry_orders_create</c> (Per-Weight order creation).
/// No store_id field: unlike pos_orders_create, this endpoint never reads one from the payload -
/// it always resolves the signed-in user's own fixed store and fails for a floating-store account
/// (see README's Known gaps).</summary>
public sealed class CreateLaundryOrderRequest
{
    [JsonPropertyName("client_request_id")]
    public string ClientRequestId { get; set; } = "";

    [JsonPropertyName("customer_id")]
    public int CustomerId { get; set; }

    [JsonPropertyName("store_id")]
    public int? StoreId { get; set; }

    [JsonPropertyName("batches")]
    public List<CreateLaundryBatchLine> Batches { get; set; } = new();

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

    [JsonPropertyName("payments")]
    public List<PaymentLine> Payments { get; set; } = new();

    [JsonPropertyName("wallet_amount")]
    public decimal WalletAmount { get; set; }
}

public sealed class CreateLaundryOrderResult
{
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("grand_total")]
    public decimal GrandTotal { get; set; }

    [JsonPropertyName("warnings")]
    public List<string> Warnings { get; set; } = new();
}
