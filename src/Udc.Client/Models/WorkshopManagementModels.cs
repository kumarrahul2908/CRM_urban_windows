using System.Text.Json.Serialization;

namespace Udc.Client.Models;

// ---- workshop_schedule ----------------------------------------------------------------------

public sealed class ScheduleOrderRow
{
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("customer")]
    public string? Customer { get; set; }

    [JsonPropertyName("due_time")]
    public string? DueTime { get; set; }

    [JsonPropertyName("garments")]
    public int Garments { get; set; }

    [JsonPropertyName("bill_value")]
    public decimal BillValue { get; set; }

    [JsonPropertyName("statuses")]
    public List<string> Statuses { get; set; } = new();
}

public sealed class ScheduleStoreBlock
{
    [JsonPropertyName("store_name")]
    public string? StoreName { get; set; }

    [JsonPropertyName("orders")]
    public List<ScheduleOrderRow> Orders { get; set; } = new();

    [JsonPropertyName("garments")]
    public int Garments { get; set; }

    [JsonPropertyName("bill_value")]
    public decimal BillValue { get; set; }
}

public sealed class ScheduleOverdueRow
{
    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("customer")]
    public string? Customer { get; set; }

    [JsonPropertyName("garment")]
    public string? Garment { get; set; }

    [JsonPropertyName("qty")]
    public int Qty { get; set; }

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("days")]
    public int Days { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }
}

public sealed class ScheduleOverdueStore
{
    [JsonPropertyName("store_name")]
    public string? StoreName { get; set; }

    [JsonPropertyName("rows")]
    public List<ScheduleOverdueRow> Rows { get; set; } = new();

    [JsonPropertyName("total_rows")]
    public int TotalRows { get; set; }
}

public sealed class ScheduleTotals
{
    [JsonPropertyName("orders")]
    public int Orders { get; set; }

    [JsonPropertyName("garments")]
    public int Garments { get; set; }

    [JsonPropertyName("bill_value")]
    public decimal BillValue { get; set; }
}

/// <summary><c>GET /api/mobile/workshop_schedule</c> (workshop.manage) - today/tomorrow's due
/// orders by store, plus everything overdue, for a Workshop Staff login scoped to just their own
/// workshop's items (see wsScheduleData()).</summary>
public sealed class WorkshopScheduleResult
{
    [JsonPropertyName("today_date")]
    public string? TodayDate { get; set; }

    [JsonPropertyName("tomorrow_date")]
    public string? TomorrowDate { get; set; }

    [JsonPropertyName("today")]
    public List<ScheduleStoreBlock> Today { get; set; } = new();

    [JsonPropertyName("tomorrow")]
    public List<ScheduleStoreBlock> Tomorrow { get; set; } = new();

    [JsonPropertyName("overdue")]
    public List<ScheduleOverdueStore> Overdue { get; set; } = new();

    [JsonPropertyName("today_totals")]
    public ScheduleTotals TodayTotals { get; set; } = new();

    [JsonPropertyName("tomorrow_totals")]
    public ScheduleTotals TomorrowTotals { get; set; } = new();

    [JsonPropertyName("overdue_totals")]
    public ScheduleTotals OverdueTotals { get; set; } = new();
}

// ---- workshop_report -----------------------------------------------------------------------

public sealed class ReportGarmentCount
{
    [JsonPropertyName("garment_name")]
    public string? GarmentName { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public sealed class WorkshopReportStoreRow
{
    [JsonPropertyName("store_id")]
    public int StoreId { get; set; }

    [JsonPropertyName("store_name")]
    public string? StoreName { get; set; }

    [JsonPropertyName("total_pieces")]
    public int TotalPieces { get; set; }

    [JsonPropertyName("garments")]
    public List<ReportGarmentCount> Garments { get; set; } = new();

    [JsonPropertyName("customers")]
    public List<string> Customers { get; set; } = new();
}

/// <summary><c>GET /api/mobile/workshop_report</c> (workshop.manage) - pieces sent out by store
/// over a period (week/15days/30days/custom), with a per-store garment breakdown and customer
/// list.</summary>
public sealed class WorkshopReportResult
{
    [JsonPropertyName("period")]
    public string? Period { get; set; }

    [JsonPropertyName("from")]
    public string? From { get; set; }

    [JsonPropertyName("to")]
    public string? To { get; set; }

    [JsonPropertyName("report")]
    public List<WorkshopReportStoreRow> Report { get; set; } = new();

    [JsonPropertyName("stores")]
    public List<WorkshopOption> Stores { get; set; } = new();
}

// ---- workshop billing -----------------------------------------------------------------------

public sealed class WorkshopBillRow
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("bill_number")]
    public string? BillNumber { get; set; }

    [JsonPropertyName("store_name")]
    public string? StoreName { get; set; }

    [JsonPropertyName("workshop_name")]
    public string? WorkshopName { get; set; }

    [JsonPropertyName("period_start")]
    public string? PeriodStart { get; set; }

    [JsonPropertyName("period_end")]
    public string? PeriodEnd { get; set; }

    [JsonPropertyName("total_value")]
    public decimal TotalValue { get; set; }

    [JsonPropertyName("workshop_share")]
    public decimal WorkshopShare { get; set; }

    [JsonPropertyName("store_share")]
    public decimal StoreShare { get; set; }

    [JsonPropertyName("share_percent")]
    public decimal SharePercent { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("invoice_date")]
    public string? InvoiceDate { get; set; }

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("amount_paid")]
    public decimal AmountPaid { get; set; }

    [JsonPropertyName("balance_due")]
    public decimal BalanceDue { get; set; }
}

public sealed class WorkshopBillingWorkshopOption
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("workshop_name")]
    public string WorkshopName { get; set; } = "";

    [JsonPropertyName("commission_workshop_percent")]
    public decimal CommissionWorkshopPercent { get; set; }
}

/// <summary><c>GET /api/mobile/workshop_billing_list</c> (workshop.billing | billing.view).
/// <see cref="CanManageBills"/> gates generate/mark-paid - a Workshop Staff login not linked to a
/// workshop gets an empty list and everything disabled (see wsBillingScope()).</summary>
public sealed class WorkshopBillingListResult
{
    [JsonPropertyName("can_manage_bills")]
    public bool CanManageBills { get; set; }

    [JsonPropertyName("bills")]
    public List<WorkshopBillRow> Bills { get; set; } = new();

    [JsonPropertyName("stores")]
    public List<WorkshopOption> Stores { get; set; } = new();

    [JsonPropertyName("workshops")]
    public List<WorkshopBillingWorkshopOption> Workshops { get; set; } = new();

    [JsonPropertyName("payment_types")]
    public List<WorkshopOption> PaymentTypes { get; set; } = new();
}

public sealed class WorkshopBillItem
{
    [JsonPropertyName("bill_item_id")]
    public int BillItemId { get; set; }

    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("garment_name")]
    public string? GarmentName { get; set; }

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("item_value")]
    public decimal ItemValue { get; set; }
}

public sealed class WorkshopBillPayment
{
    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("payment_type_name")]
    public string? PaymentTypeName { get; set; }

    [JsonPropertyName("reference_no")]
    public string? ReferenceNo { get; set; }

    [JsonPropertyName("paid_on")]
    public string? PaidOn { get; set; }

    [JsonPropertyName("notes")]
    public string? Notes { get; set; }
}

public sealed class WorkshopBillDetail
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("bill_number")]
    public string? BillNumber { get; set; }

    [JsonPropertyName("store_name")]
    public string? StoreName { get; set; }

    [JsonPropertyName("workshop_name")]
    public string? WorkshopName { get; set; }

    [JsonPropertyName("period_start")]
    public string? PeriodStart { get; set; }

    [JsonPropertyName("period_end")]
    public string? PeriodEnd { get; set; }

    [JsonPropertyName("total_value")]
    public decimal TotalValue { get; set; }

    [JsonPropertyName("share_percent")]
    public decimal SharePercent { get; set; }

    [JsonPropertyName("workshop_share")]
    public decimal WorkshopShare { get; set; }

    [JsonPropertyName("store_share")]
    public decimal StoreShare { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("payment_type_name")]
    public string? PaymentTypeName { get; set; }

    [JsonPropertyName("reference_no")]
    public string? ReferenceNo { get; set; }

    [JsonPropertyName("invoice_date")]
    public string? InvoiceDate { get; set; }

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("amount_paid")]
    public decimal AmountPaid { get; set; }

    [JsonPropertyName("balance_due")]
    public decimal BalanceDue { get; set; }

    [JsonPropertyName("gst_mode")]
    public string? GstMode { get; set; }

    [JsonPropertyName("gst_rate")]
    public decimal GstRate { get; set; }

    [JsonPropertyName("gst_amount")]
    public decimal GstAmount { get; set; }

    [JsonPropertyName("grand_total")]
    public decimal GrandTotal { get; set; }

    [JsonPropertyName("billing_basis")]
    public string? BillingBasis { get; set; }
}

/// <summary><c>GET /api/mobile/workshop_billing_detail</c>.</summary>
public sealed class WorkshopBillingDetailResult
{
    [JsonPropertyName("can_manage_bills")]
    public bool CanManageBills { get; set; }

    [JsonPropertyName("bill")]
    public WorkshopBillDetail Bill { get; set; } = new();

    [JsonPropertyName("items")]
    public List<WorkshopBillItem> Items { get; set; } = new();

    [JsonPropertyName("payments")]
    public List<WorkshopBillPayment> Payments { get; set; } = new();
}

/// <summary>Body for <c>POST /api/mobile/workshop_billing_generate</c>. <see cref="StoreId"/> is a
/// string because the backend accepts the literal <c>"all"</c> (a combined invoice for every
/// store) as well as a numeric id.</summary>
public sealed class GenerateWorkshopBillRequest
{
    [JsonPropertyName("store_id")]
    public string StoreId { get; set; } = "";

    [JsonPropertyName("workshop_id")]
    public int WorkshopId { get; set; }

    [JsonPropertyName("from")]
    public string From { get; set; } = "";

    [JsonPropertyName("to")]
    public string To { get; set; } = "";

    [JsonPropertyName("gst_mode")]
    public string GstMode { get; set; } = "none";

    [JsonPropertyName("basis")]
    public string Basis { get; set; } = "whole";
}

public sealed class GenerateWorkshopBillResult
{
    [JsonPropertyName("bill_id")]
    public int BillId { get; set; }

    [JsonPropertyName("bill_number")]
    public string? BillNumber { get; set; }
}

/// <summary>Body for <c>POST /api/mobile/workshop_billing_mark_paid</c>. Leave
/// <see cref="Amount"/> at 0 to pay the full remaining balance (the backend's own default).</summary>
public sealed class MarkWorkshopBillPaidRequest
{
    [JsonPropertyName("bill_id")]
    public int BillId { get; set; }

    [JsonPropertyName("payment_type_id")]
    public int PaymentTypeId { get; set; }

    [JsonPropertyName("amount")]
    public decimal Amount { get; set; }

    [JsonPropertyName("reference_no")]
    public string ReferenceNo { get; set; } = "";
}

public sealed class MarkWorkshopBillPaidResult
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }
}
