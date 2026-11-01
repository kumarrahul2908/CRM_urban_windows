using System.Text.Json.Serialization;

namespace Udc.Client.Models;

/// <summary>One row of <c>workshop_queue</c>'s <c>piece.items</c> - an order item that's
/// pending, out at the workshop, or being processed (see src/routes/mobile/workshop.ts).</summary>
public sealed class WorkshopPieceItem
{
    [JsonPropertyName("order_item_id")]
    public int OrderItemId { get; set; }

    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("item_status")]
    public string ItemStatus { get; set; } = "";

    [JsonPropertyName("quantity")]
    public decimal? Quantity { get; set; }

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("garment_name")]
    public string? GarmentName { get; set; }

    [JsonPropertyName("service_name")]
    public string? ServiceName { get; set; }

    [JsonPropertyName("urgent_level_name")]
    public string? UrgentLevelName { get; set; }

    [JsonPropertyName("workshop_item_id")]
    public int? WorkshopItemId { get; set; }

    [JsonPropertyName("workshop_status")]
    public string? WorkshopStatus { get; set; }

    /// <summary>History-derived true status (see WorkshopService.getCurrentStatus()) - the
    /// source of truth for which action, if any, applies next.</summary>
    [JsonPropertyName("current_status")]
    public string? CurrentStatus { get; set; }

    [JsonPropertyName("challan_number")]
    public string? ChallanNumber { get; set; }
}

public sealed class WorkshopPieceQueue
{
    [JsonPropertyName("items")]
    public List<WorkshopPieceItem> Items { get; set; } = new();

    [JsonPropertyName("total")]
    public int Total { get; set; }

    [JsonPropertyName("page")]
    public int Page { get; set; }

    [JsonPropertyName("total_pages")]
    public int TotalPages { get; set; }
}

/// <summary>One row of the accept/return/store-receive queues.</summary>
public sealed class WorkshopActionQueueItem
{
    [JsonPropertyName("workshop_item_id")]
    public int WorkshopItemId { get; set; }

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "piece";

    [JsonPropertyName("garment_name")]
    public string? GarmentName { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("current_status")]
    public string? CurrentStatus { get; set; }
}

public sealed class WorkshopOption
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = "";
}

/// <summary><c>GET /api/mobile/workshop_queue</c>. Laundry (per-weight) lists aren't modeled
/// here yet - this app's workshop screen covers piece items only (see README's Known gaps).</summary>
public sealed class WorkshopQueueResult
{
    [JsonPropertyName("can_create_challan")]
    public bool CanCreateChallan { get; set; }

    [JsonPropertyName("piece")]
    public WorkshopPieceQueue Piece { get; set; } = new();

    [JsonPropertyName("accept_queue")]
    public List<WorkshopActionQueueItem> AcceptQueue { get; set; } = new();

    [JsonPropertyName("return_queue")]
    public List<WorkshopActionQueueItem> ReturnQueue { get; set; } = new();

    [JsonPropertyName("store_receive_queue")]
    public List<WorkshopActionQueueItem> StoreReceiveQueue { get; set; } = new();

    [JsonPropertyName("workshops")]
    public List<WorkshopOption> Workshops { get; set; } = new();
}

/// <summary>Body for <c>POST /api/mobile/workshop_action</c> with one of the item-lifecycle
/// actions (start_processing, complete_washing, pass_qc, complete_ironing, complete_processing,
/// accept, return_to_store, store_receive, force_receive, mark_unprocessed, resolve_unprocessed).</summary>
public sealed class WorkshopItemActionRequest
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("workshop_item_id")]
    public int WorkshopItemId { get; set; }

    [JsonPropertyName("remarks")]
    public string Remarks { get; set; } = "";
}

/// <summary>Body for <c>POST /api/mobile/workshop_action</c> with action=create_challan
/// (send selected Pending piece items to a workshop).</summary>
public sealed class CreateChallanRequest
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "create_challan";

    [JsonPropertyName("workshop_id")]
    public int WorkshopId { get; set; }

    [JsonPropertyName("item_ids")]
    public List<int> ItemIds { get; set; } = new();

    [JsonPropertyName("remarks")]
    public string Remarks { get; set; } = "";
}

public sealed class WorkshopActionResult
{
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("workshop_item_id")]
    public int? WorkshopItemId { get; set; }

    [JsonPropertyName("challan_number")]
    public string? ChallanNumber { get; set; }
}

/// <summary><c>GET /api/mobile/barcode_lookup</c> (also used for qr_lookup's response shape).</summary>
public sealed class BarcodeLookupResult
{
    [JsonPropertyName("order_mode")]
    public string OrderMode { get; set; } = "piece";

    [JsonPropertyName("order_id")]
    public int OrderId { get; set; }

    [JsonPropertyName("order_number")]
    public string? OrderNumber { get; set; }

    [JsonPropertyName("invoice_number")]
    public string? InvoiceNumber { get; set; }

    [JsonPropertyName("sequence")]
    public int? Sequence { get; set; }

    [JsonPropertyName("total_garments")]
    public int? TotalGarments { get; set; }

    [JsonPropertyName("customer_name")]
    public string? CustomerName { get; set; }

    [JsonPropertyName("customer_mobile")]
    public string? CustomerMobile { get; set; }

    [JsonPropertyName("garment_name")]
    public string? GarmentName { get; set; }

    [JsonPropertyName("service_name")]
    public string? ServiceName { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("due_date")]
    public string? DueDate { get; set; }

    [JsonPropertyName("remarks")]
    public string? Remarks { get; set; }
}
