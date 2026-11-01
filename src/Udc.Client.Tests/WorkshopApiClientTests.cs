using System.Net;
using Udc.Client;
using Udc.Client.Models;
using Xunit;

namespace Udc.Client.Tests;

public class WorkshopApiClientTests
{
    [Fact]
    public async Task GetWorkshopQueueAsync_ParsesPieceItemsAndQueues()
    {
        var body = """
        {"success":true,"data":{
          "can_create_challan":true,
          "piece":{"items":[{"order_item_id":1,"order_id":10,"order_number":"ST-0010","item_status":"Pending",
            "quantity":2,"due_date":"2026-11-05","customer_name":"Asha Rao","garment_name":"Shirt",
            "service_name":"Wash & Iron","workshop_item_id":null,"workshop_status":null,"current_status":null}],
            "total":1,"page":1,"total_pages":1},
          "laundry_pending":[],"laundry_in_progress":[],
          "accept_queue":[{"workshop_item_id":5,"kind":"piece","garment_name":"Trouser","order_number":"ST-0011",
            "customer_name":"Ravi Kumar","current_status":"Received"}],
          "return_queue":[],"store_receive_queue":[],
          "workshops":[{"id":1,"name":"Main Workshop"}]}}
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.GetWorkshopQueueAsync(order: "ST-00", status: "Pending");

        Assert.True(result.CanCreateChallan);
        Assert.Single(result.Piece.Items);
        Assert.Equal("Shirt", result.Piece.Items[0].GarmentName);
        Assert.Single(result.AcceptQueue);
        Assert.Equal("Received", result.AcceptQueue[0].CurrentStatus);
        Assert.Single(result.Workshops);
        Assert.Equal("?order=ST-00&status=Pending&page=1", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GetWorkshopQueueAsync_ParsesLaundryPendingAndInProgress()
    {
        var body = """
        {"success":true,"data":{
          "can_create_challan":true,
          "piece":{"items":[],"total":0,"page":1,"total_pages":0},
          "laundry_pending":[{"laundry_garment_id":1,"sequence":1,"garment_name":"Shirt","total_garments":3,
            "order_id":20,"order_number":"ST-0020","due_date":"2026-11-06","customer_name":"Asha Rao",
            "service_name":"Wash & Fold","status":"Pending","current_status":null,"workshop_item_id":null}],
          "laundry_in_progress":[{"laundry_garment_id":2,"sequence":2,"garment_name":"Trouser","total_garments":3,
            "order_id":20,"order_number":"ST-0020","due_date":"2026-11-06","customer_name":"Asha Rao",
            "service_name":"Wash & Fold","status":"Workshop","current_status":"Received","workshop_item_id":9}],
          "accept_queue":[],"return_queue":[],"store_receive_queue":[],
          "workshops":[{"id":1,"name":"Main Workshop"}]}}
        """;
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.GetWorkshopQueueAsync();

        Assert.Single(result.LaundryPending);
        Assert.Null(result.LaundryPending[0].WorkshopItemId);
        Assert.Single(result.LaundryInProgress);
        Assert.Equal(9, result.LaundryInProgress[0].WorkshopItemId);
        Assert.Equal("Received", result.LaundryInProgress[0].CurrentStatus);
    }

    [Fact]
    public async Task SendWorkshopItemActionAsync_SendsActionAndWorkshopItemId()
    {
        var body = """{"success":true,"data":{"message":"Processing started.","workshop_item_id":5}}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.SendWorkshopItemActionAsync(new WorkshopItemActionRequest { Action = "start_processing", WorkshopItemId = 5 });

        Assert.Equal("Processing started.", result.Message);
        Assert.Equal("/api/mobile/workshop_action", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task SendWorkshopItemActionAsync_InvalidTransition_ThrowsWithServerMessage()
    {
        var body = """{"success":false,"error":{"code":"action_failed","message":"This item is at status \"Sent\" — cannot perform this action from its current state."}}""";
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.UnprocessableEntity, body));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.SendWorkshopItemActionAsync(new WorkshopItemActionRequest { Action = "accept", WorkshopItemId = 5 }));

        Assert.Equal("action_failed", ex.Code);
        Assert.Contains("cannot perform this action", ex.Message);
    }

    [Fact]
    public async Task CreateChallanAsync_SendsItemIdsAndWorkshop()
    {
        var body = """{"success":true,"data":{"message":"Challan CH-0007 created with 2 item(s).","challan_number":"CH-0007"}}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.CreateChallanAsync(new CreateChallanRequest { WorkshopId = 1, ItemIds = new List<int> { 1, 2 } });

        Assert.Equal("CH-0007", result.ChallanNumber);
    }

    [Fact]
    public async Task CreateLaundryChallanAsync_SendsLaundryGarmentIdsAndWorkshop()
    {
        var body = """{"success":true,"data":{"message":"Challan CH-0008 created with 2 item(s).","challan_number":"CH-0008"}}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.CreateLaundryChallanAsync(new CreateLaundryChallanRequest { WorkshopId = 1, LaundryGarmentIds = new List<int> { 1, 2 } });

        Assert.Equal("CH-0008", result.ChallanNumber);
        Assert.Equal("/api/mobile/workshop_action", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task LookupBarcodeAsync_ParsesPieceResult()
    {
        var body = """
        {"success":true,"data":{"order_mode":"piece","order_item_id":1,"order_id":10,"order_number":"ST-0010",
          "invoice_number":"INV-0010","sequence":1,"customer_name":"Asha Rao","customer_mobile":"9000000000",
          "garment_name":"Shirt","service_name":"Wash & Iron","status":"Processing","due_date":"2026-11-05","remarks":null}}
        """;
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.LookupBarcodeAsync("ABC123");

        Assert.Equal("piece", result.OrderMode);
        Assert.Equal("Shirt", result.GarmentName);
        Assert.Equal("Processing", result.Status);
    }

    [Fact]
    public async Task LookupBarcodeAsync_NotFound_ThrowsWithServerMessage()
    {
        var body = """{"success":false,"error":{"code":"not_found","message":"No garment found for this barcode."}}""";
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.NotFound, body));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.LookupBarcodeAsync("DOES-NOT-EXIST"));

        Assert.Equal("not_found", ex.Code);
    }
}
