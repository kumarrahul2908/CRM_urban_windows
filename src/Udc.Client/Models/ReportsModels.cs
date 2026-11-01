using System.Text.Json.Serialization;

namespace Udc.Client.Models;

// ---- reports_summary ----------------------------------------------------------------------

public sealed class ReportStatusCount
{
    [JsonPropertyName("status_label")]
    public string? StatusLabel { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public sealed class ServiceStat
{
    [JsonPropertyName("service_name")]
    public string? ServiceName { get; set; }

    [JsonPropertyName("billing_unit")]
    public string? BillingUnit { get; set; }

    [JsonPropertyName("item_count")]
    public int ItemCount { get; set; }

    [JsonPropertyName("revenue")]
    public decimal Revenue { get; set; }
}

public sealed class GarmentStat
{
    [JsonPropertyName("garment_name")]
    public string? GarmentName { get; set; }

    [JsonPropertyName("item_count")]
    public int ItemCount { get; set; }

    [JsonPropertyName("revenue")]
    public decimal? Revenue { get; set; }
}

public sealed class SalesSummary
{
    [JsonPropertyName("order_count")]
    public int OrderCount { get; set; }

    [JsonPropertyName("order_value")]
    public decimal OrderValue { get; set; }

    [JsonPropertyName("avg_order_value")]
    public decimal AvgOrderValue { get; set; }

    [JsonPropertyName("by_status")]
    public List<ReportStatusCount> ByStatus { get; set; } = new();

    [JsonPropertyName("by_service")]
    public List<ServiceStat> ByService { get; set; } = new();

    [JsonPropertyName("top_garments")]
    public List<GarmentStat> TopGarments { get; set; } = new();
}

public sealed class PaymentTypeTotal
{
    [JsonPropertyName("type_name")]
    public string TypeName { get; set; } = "";

    [JsonPropertyName("total")]
    public decimal Total { get; set; }
}

public sealed class OpenReceivablesSplit
{
    [JsonPropertyName("paid")]
    public int Paid { get; set; }

    [JsonPropertyName("partial")]
    public int Partial { get; set; }

    [JsonPropertyName("unpaid")]
    public int Unpaid { get; set; }
}

public sealed class PaymentsSummary
{
    [JsonPropertyName("collections")]
    public List<PaymentTypeTotal> Collections { get; set; } = new();

    [JsonPropertyName("refunds")]
    public List<PaymentTypeTotal> Refunds { get; set; } = new();

    [JsonPropertyName("total_collected")]
    public decimal TotalCollected { get; set; }

    [JsonPropertyName("total_refunded")]
    public decimal TotalRefunded { get; set; }

    [JsonPropertyName("advance_deposits")]
    public decimal AdvanceDeposits { get; set; }

    [JsonPropertyName("open_receivables")]
    public OpenReceivablesSplit OpenReceivables { get; set; } = new();
}

public sealed class TopCustomerStat
{
    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("order_count")]
    public int OrderCount { get; set; }

    [JsonPropertyName("revenue")]
    public decimal Revenue { get; set; }

    [JsonPropertyName("outstanding")]
    public decimal Outstanding { get; set; }
}

public sealed class CustomersSummary
{
    [JsonPropertyName("new_customers")]
    public int NewCustomers { get; set; }

    [JsonPropertyName("returning_customers")]
    public int ReturningCustomers { get; set; }

    [JsonPropertyName("top_customers")]
    public List<TopCustomerStat> TopCustomers { get; set; } = new();
}

public sealed class OperationsSummary
{
    [JsonPropertyName("in_workshop")]
    public int InWorkshop { get; set; }

    [JsonPropertyName("processing")]
    public int Processing { get; set; }

    [JsonPropertyName("qc_pending")]
    public int QcPending { get; set; }

    [JsonPropertyName("in_rework")]
    public int InRework { get; set; }

    [JsonPropertyName("ready_to_pack")]
    public int ReadyToPack { get; set; }

    [JsonPropertyName("packed")]
    public int Packed { get; set; }

    [JsonPropertyName("qc_passed")]
    public int QcPassed { get; set; }

    [JsonPropertyName("qc_failed")]
    public int QcFailed { get; set; }

    [JsonPropertyName("overdue_orders")]
    public int OverdueOrders { get; set; }

    [JsonPropertyName("urgent_orders")]
    public int UrgentOrders { get; set; }
}

/// <summary><c>GET /api/mobile/reports_summary</c> - a full business overview for a date range.</summary>
public sealed class ReportsSummaryResult
{
    [JsonPropertyName("date_from")]
    public string? DateFrom { get; set; }

    [JsonPropertyName("date_to")]
    public string? DateTo { get; set; }

    [JsonPropertyName("sales")]
    public SalesSummary Sales { get; set; } = new();

    [JsonPropertyName("payments")]
    public PaymentsSummary Payments { get; set; } = new();

    [JsonPropertyName("customers")]
    public CustomersSummary Customers { get; set; } = new();

    [JsonPropertyName("operations")]
    public OperationsSummary Operations { get; set; } = new();
}

// ---- outstanding_list -----------------------------------------------------------------------

public sealed class ReceivableRow
{
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("total_amount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("paid_amount")]
    public decimal PaidAmount { get; set; }

    [JsonPropertyName("due_amount")]
    public decimal DueAmount { get; set; }

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("ageing_bucket")]
    public string? AgeingBucket { get; set; }
}

public sealed class CustomerOutstandingRow
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("orders")]
    public int Orders { get; set; }

    [JsonPropertyName("total_due")]
    public decimal TotalDue { get; set; }
}

/// <summary><c>GET /api/mobile/outstanding_list</c>.</summary>
public sealed class OutstandingListResult
{
    [JsonPropertyName("receivables")]
    public List<ReceivableRow> Receivables { get; set; } = new();

    [JsonPropertyName("bucket_totals")]
    public Dictionary<string, decimal> BucketTotals { get; set; } = new();

    [JsonPropertyName("grand_outstanding")]
    public decimal GrandOutstanding { get; set; }

    [JsonPropertyName("by_customer")]
    public List<CustomerOutstandingRow> ByCustomer { get; set; } = new();
}

// ---- daily_collection -----------------------------------------------------------------------

public sealed class CollectionDetail
{
    [JsonPropertyName("payment_number")]
    public string? PaymentNumber { get; set; }

    [JsonPropertyName("payment_date")]
    public string? PaymentDate { get; set; }

    [JsonPropertyName("payment_kind")]
    public string? PaymentKind { get; set; }

    [JsonPropertyName("total_amount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("created_by_name")]
    public string? CreatedByName { get; set; }

    [JsonPropertyName("breakdown")]
    public string? Breakdown { get; set; }
}

/// <summary><c>GET /api/mobile/daily_collection</c>.</summary>
public sealed class DailyCollectionResult
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("collections")]
    public List<PaymentTypeTotal> Collections { get; set; } = new();

    [JsonPropertyName("refunds")]
    public List<PaymentTypeTotal> Refunds { get; set; } = new();

    [JsonPropertyName("total_collections")]
    public decimal TotalCollections { get; set; }

    [JsonPropertyName("total_refunds")]
    public decimal TotalRefunds { get; set; }

    [JsonPropertyName("net_collection")]
    public decimal NetCollection { get; set; }

    [JsonPropertyName("advance_received")]
    public decimal AdvanceReceived { get; set; }

    [JsonPropertyName("details")]
    public List<CollectionDetail> Details { get; set; } = new();
}
