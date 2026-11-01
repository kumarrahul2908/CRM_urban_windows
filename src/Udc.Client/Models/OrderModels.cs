using System.Text.Json.Serialization;

namespace Udc.Client.Models;

/// <summary>One row of <c>GET /api/mobile/orders_list</c>.</summary>
public sealed class OrderSummary
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("order_number")]
    public string OrderNumber { get; set; } = "";

    [JsonPropertyName("invoice_number")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("order_date")]
    public string? OrderDate { get; set; }

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("order_mode")]
    public string? OrderMode { get; set; }

    [JsonPropertyName("grand_total")]
    public decimal GrandTotal { get; set; }

    [JsonPropertyName("total_paid")]
    public decimal TotalPaid { get; set; }

    [JsonPropertyName("current_due")]
    public decimal CurrentDue { get; set; }

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("customer_mobile")]
    public string? CustomerMobile { get; set; }

    [JsonPropertyName("status_label")]
    public string? StatusLabel { get; set; }

    [JsonPropertyName("status_key")]
    public string? StatusKey { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }
}

public sealed class OrderListResult
{
    [JsonPropertyName("results")]
    public List<OrderSummary> Results { get; set; } = new();

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }
}

/// <summary>The flat <c>order</c> object inside <c>GET /api/mobile/orders_detail</c>.</summary>
public sealed class OrderHeader
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("order_number")]
    public string OrderNumber { get; set; } = "";

    [JsonPropertyName("invoice_number")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("customer_id")]
    public int CustomerId { get; set; }

    [JsonPropertyName("store_name")]
    public string? StoreName { get; set; }

    [JsonPropertyName("order_mode")]
    public string? OrderMode { get; set; }

    [JsonPropertyName("order_date")]
    public string? OrderDate { get; set; }

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("due_time")]
    public string? DueTime { get; set; }

    [JsonPropertyName("urgent_level_name")]
    public string? UrgentLevelName { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }

    [JsonPropertyName("home_delivery")]
    public bool HomeDelivery { get; set; }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonPropertyName("discount_amount")]
    public decimal DiscountAmount { get; set; }

    [JsonPropertyName("urgent_charge")]
    public decimal UrgentCharge { get; set; }

    [JsonPropertyName("tax_amount")]
    public decimal TaxAmount { get; set; }

    [JsonPropertyName("delivery_charge")]
    public decimal DeliveryCharge { get; set; }

    [JsonPropertyName("grand_total")]
    public decimal GrandTotal { get; set; }

    [JsonPropertyName("total_paid")]
    public decimal TotalPaid { get; set; }

    [JsonPropertyName("current_due")]
    public decimal CurrentDue { get; set; }

    [JsonPropertyName("status_key")]
    public string? StatusKey { get; set; }

    [JsonPropertyName("status_label")]
    public string? StatusLabel { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("customer_mobile")]
    public string? CustomerMobile { get; set; }

    [JsonPropertyName("customer_address")]
    public string? CustomerAddress { get; set; }
}

public sealed class OrderActions
{
    [JsonPropertyName("can_edit")]
    public bool CanEdit { get; set; }

    [JsonPropertyName("can_add_item")]
    public bool CanAddItem { get; set; }

    [JsonPropertyName("can_remove_item")]
    public bool CanRemoveItem { get; set; }

    [JsonPropertyName("can_cancel")]
    public bool CanCancel { get; set; }
}

/// <summary>One row of <c>order_items</c> (superset of piece/weight modes - unused fields for a
/// given mode are simply absent/zero). See db/schema/schema.sql's order_items table.</summary>
public sealed class OrderItemRow
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("garment_name")]
    public string? GarmentName { get; set; }

    [JsonPropertyName("service_name")]
    public string? ServiceName { get; set; }

    [JsonPropertyName("brand_name")]
    public string? BrandName { get; set; }

    [JsonPropertyName("color_name")]
    public string? ColorName { get; set; }

    [JsonPropertyName("quantity")]
    public decimal Quantity { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("unit_price")]
    public decimal UnitPrice { get; set; }

    [JsonPropertyName("weight")]
    public decimal? Weight { get; set; }

    [JsonPropertyName("weight_rate")]
    public decimal? WeightRate { get; set; }

    [JsonPropertyName("subtotal")]
    public decimal Subtotal { get; set; }

    [JsonPropertyName("total")]
    public decimal Total { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }

    [JsonPropertyName("item_status")]
    public string? ItemStatus { get; set; }

    [JsonPropertyName("defects")]
    public List<string> Defects { get; set; } = new();
}

public sealed class LaundryGarmentRow
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("garment_name")]
    public string? GarmentName { get; set; }

    [JsonPropertyName("item_sequence")]
    public int? ItemSequence { get; set; }
}

public sealed class PaymentRow
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("payment_number")]
    public string? PaymentNumber { get; set; }

    [JsonPropertyName("payment_date")]
    public string? PaymentDate { get; set; }

    [JsonPropertyName("total_amount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("payment_kind")]
    public string? PaymentKind { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }

    [JsonPropertyName("created_by_name")]
    public string? CreatedByName { get; set; }

    [JsonPropertyName("breakdown")]
    public string? Breakdown { get; set; }
}

public sealed class OrderDetailResult
{
    [JsonPropertyName("order")]
    public OrderHeader Order { get; set; } = new();

    [JsonPropertyName("order_actions")]
    public OrderActions OrderActions { get; set; } = new();

    [JsonPropertyName("receivable_status")]
    public string? ReceivableStatus { get; set; }

    [JsonPropertyName("items")]
    public List<OrderItemRow> Items { get; set; } = new();

    [JsonPropertyName("laundry_garments")]
    public List<LaundryGarmentRow> LaundryGarments { get; set; } = new();

    [JsonPropertyName("payments")]
    public List<PaymentRow> Payments { get; set; } = new();

    [JsonPropertyName("wallet_balance")]
    public decimal WalletBalance { get; set; }
}

public sealed class PaymentTypeOption
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("type_name")]
    public string TypeName { get; set; } = "";
}

/// <summary>Only the field used by the Record Payment screen; the endpoint itself returns a
/// full payment listing too.</summary>
public sealed class PaymentTypesResult
{
    [JsonPropertyName("payment_types")]
    public List<PaymentTypeOption> PaymentTypes { get; set; } = new();
}

/// <summary>Body for <c>POST /api/mobile/orders_record_payment</c>. Phase 2 supports a single
/// payment line (the common case) - multi-line split payments are a later-phase addition.</summary>
public sealed class RecordPaymentRequest
{
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("client_request_id")]
    public string ClientRequestId { get; set; } = "";

    [JsonPropertyName("payment_lines")]
    public List<PaymentLine> PaymentLines { get; set; } = new();

    [JsonPropertyName("wallet_amount")]
    public decimal WalletAmount { get; set; }
}

public sealed class PaymentLine
{
    [JsonPropertyName("payment_type_id")]
    public int PaymentTypeId { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }
}

public sealed class RecordPaymentResult
{
    [JsonPropertyName("payment_id")]
    public int PaymentId { get; set; }

    [JsonPropertyName("payment_number")]
    public string? PaymentNumber { get; set; }

    [JsonPropertyName("updated_due")]
    public decimal UpdatedDue { get; set; }

    [JsonPropertyName("idempotent_replay")]
    public bool IdempotentReplay { get; set; }
}
