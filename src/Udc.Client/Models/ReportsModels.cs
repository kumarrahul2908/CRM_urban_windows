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

// ---- audit_logs (Super Admin / Admin only - audit.view) ----------------------------------

/// <summary>One row of <c>audit_logs</c>' <c>entries</c>. <see cref="OldValue"/>/<see cref="NewValue"/>
/// are already redacted server-side (secrets/PII) before this app ever sees them - see
/// auditViewer.ts's redactValue()/redactNode().</summary>
public sealed class AuditLogEntry
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("actor")]
    public string? Actor { get; set; }

    [JsonPropertyName("module")]
    public string? Module { get; set; }

    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("record_id")]
    public string? RecordId { get; set; }

    [JsonPropertyName("old_value")]
    public string? OldValue { get; set; }

    [JsonPropertyName("new_value")]
    public string? NewValue { get; set; }

    [JsonPropertyName("ip_address")]
    public string? IpAddress { get; set; }
}

public sealed class AuditLogOptions
{
    [JsonPropertyName("modules")]
    public List<string> Modules { get; set; } = new();

    [JsonPropertyName("actions")]
    public List<string> Actions { get; set; } = new();
}

/// <summary><c>GET /api/mobile/audit_logs</c>. Only visible to Super Admin / Admin (audit.view +
/// a role check) - most signed-in counter-staff accounts will get a 403 opening this screen,
/// same as Workshop's workshop.manage gate.</summary>
public sealed class AuditLogsResult
{
    [JsonPropertyName("entries")]
    public List<AuditLogEntry> Entries { get; set; } = new();

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("per_page")]
    public int PerPage { get; set; }

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("options")]
    public AuditLogOptions? Options { get; set; }
}

// ---- customer_history ----------------------------------------------------------------------

public sealed class CustomerHistoryOrder
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("order_date")]
    public string? OrderDate { get; set; }

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("grand_total")]
    public decimal GrandTotal { get; set; }

    [JsonPropertyName("current_due")]
    public decimal CurrentDue { get; set; }

    [JsonPropertyName("order_mode")]
    public string? OrderMode { get; set; }

    [JsonPropertyName("status_label")]
    public string? StatusLabel { get; set; }
}

public sealed class CustomerHistoryPayment
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("payment_number")]
    public string? PaymentNumber { get; set; }

    [JsonPropertyName("order_id")]
    public int? OrderId { get; set; }

    [JsonPropertyName("payment_date")]
    public string? PaymentDate { get; set; }

    [JsonPropertyName("payment_kind")]
    public string? PaymentKind { get; set; }

    [JsonPropertyName("total_amount")]
    public decimal TotalAmount { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}

public sealed class CustomerHistoryGarment
{
    [JsonPropertyName("garment_name")]
    public string? GarmentName { get; set; }

    [JsonPropertyName("service_name")]
    public string? ServiceName { get; set; }

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("rate")]
    public decimal? Rate { get; set; }

    [JsonPropertyName("weight")]
    public decimal? Weight { get; set; }

    [JsonPropertyName("order_date")]
    public string? OrderDate { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }
}

public sealed class CustomerHistoryWalletEntry
{
    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }

    [JsonPropertyName("txn_type")]
    public string? TxnType { get; set; }

    [JsonPropertyName("debit")]
    public decimal Debit { get; set; }

    [JsonPropertyName("credit")]
    public decimal Credit { get; set; }

    [JsonPropertyName("balance_after")]
    public decimal BalanceAfter { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}

public sealed class CustomerHistoryDelivery
{
    [JsonPropertyName("delivery_number")]
    public string? DeliveryNumber { get; set; }

    [JsonPropertyName("delivery_date")]
    public string? DeliveryDate { get; set; }

    [JsonPropertyName("time_slot")]
    public string? TimeSlot { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("amount_due")]
    public decimal AmountDue { get; set; }
}

public sealed class CustomerHistoryFeedback
{
    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("rating")]
    public int? Rating { get; set; }

    [JsonPropertyName("comments")]
    public string? Comments { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("created_at")]
    public string? CreatedAt { get; set; }
}

public sealed class CustomerHistoryLedgerEntry
{
    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("debit")]
    public decimal Debit { get; set; }

    [JsonPropertyName("credit")]
    public decimal Credit { get; set; }
}

/// <summary><c>GET /api/mobile/customer_history</c> - everything about one customer in one call:
/// a combined debit/credit ledger plus the underlying orders/payments/garments/wallet/deliveries/
/// feedback lists it was built from (see reports.ts' customer_history route).</summary>
public sealed class CustomerHistoryResult
{
    [JsonPropertyName("customer_id")]
    public int CustomerId { get; set; }

    [JsonPropertyName("outstanding")]
    public decimal Outstanding { get; set; }

    [JsonPropertyName("wallet_balance")]
    public decimal WalletBalance { get; set; }

    [JsonPropertyName("orders")]
    public List<CustomerHistoryOrder> Orders { get; set; } = new();

    [JsonPropertyName("payments")]
    public List<CustomerHistoryPayment> Payments { get; set; } = new();

    [JsonPropertyName("garments")]
    public List<CustomerHistoryGarment> Garments { get; set; } = new();

    [JsonPropertyName("wallet")]
    public List<CustomerHistoryWalletEntry> Wallet { get; set; } = new();

    [JsonPropertyName("deliveries")]
    public List<CustomerHistoryDelivery> Deliveries { get; set; } = new();

    [JsonPropertyName("feedback")]
    public List<CustomerHistoryFeedback> Feedback { get; set; } = new();

    [JsonPropertyName("ledger")]
    public List<CustomerHistoryLedgerEntry> Ledger { get; set; } = new();
}

// ---- dormant_customers ----------------------------------------------------------------------

public sealed class DormantCustomerRow
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("full_name")]
    public string? FullName { get; set; }

    [JsonPropertyName("mobile")]
    public string? Mobile { get; set; }

    [JsonPropertyName("communication_preference")]
    public string? CommunicationPreference { get; set; }

    [JsonPropertyName("last_order_date")]
    public string? LastOrderDate { get; set; }

    [JsonPropertyName("days_inactive")]
    public int DaysInactive { get; set; }
}

/// <summary><c>GET /api/mobile/dormant_customers</c>.</summary>
public sealed class DormantCustomersResult
{
    [JsonPropertyName("customers")]
    public List<DormantCustomerRow> Customers { get; set; } = new();

    [JsonPropertyName("min_days")]
    public int MinDays { get; set; }

    [JsonPropertyName("max_days")]
    public int? MaxDays { get; set; }
}
