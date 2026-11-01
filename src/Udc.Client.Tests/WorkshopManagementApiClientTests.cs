using System.Net;
using Udc.Client;
using Udc.Client.Models;
using Xunit;

namespace Udc.Client.Tests;

public class WorkshopManagementApiClientTests
{
    [Fact]
    public async Task GetWorkshopScheduleAsync_ParsesTodayTomorrowAndOverdue()
    {
        var body = """
        {"success":true,"data":{
          "today_date":"2026-11-01","tomorrow_date":"2026-11-02",
          "today":[{"store_name":"Main Branch","orders":[{"order_id":10,"order_number":"ST-0010",
            "customer":"Asha Rao","due_time":"14:00","garments":3,"bill_value":450,"statuses":["Processing"]}],
            "garments":3,"bill_value":450}],
          "tomorrow":[],
          "overdue":[{"store_name":"Main Branch","rows":[{"order_id":9,"order_number":"ST-0009",
            "customer":"Ravi Kumar","garment":"Shirt","qty":1,"due_date":"2026-10-30","days":2,"status":"Workshop"}],
            "total_rows":1}],
          "today_totals":{"orders":1,"garments":3,"bill_value":450},
          "tomorrow_totals":{"orders":0,"garments":0,"bill_value":0},
          "overdue_totals":{"orders":1,"garments":1,"bill_value":0}}}
        """;
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.GetWorkshopScheduleAsync();

        Assert.Single(result.Today);
        Assert.Single(result.Today[0].Orders);
        Assert.Equal("Asha Rao", result.Today[0].Orders[0].Customer);
        Assert.Single(result.Overdue);
        Assert.Equal(2, result.Overdue[0].Rows[0].Days);
        Assert.Equal(450m, result.TodayTotals.BillValue);
    }

    [Fact]
    public async Task GetWorkshopReportAsync_BuildsQueryString_AndParsesReport()
    {
        var body = """
        {"success":true,"data":{"period":"week","from":"2026-10-26","to":"2026-11-01",
          "report":[{"store_id":1,"store_name":"Main Branch","total_pieces":12,
            "garments":[{"garment_name":"Shirt","count":8}],"customers":["Asha Rao"]}],
          "stores":[{"id":1,"name":"Main Branch"}]}}
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.GetWorkshopReportAsync(period: "30days", storeId: 1);

        Assert.Single(result.Report);
        Assert.Equal(12, result.Report[0].TotalPieces);
        Assert.Single(result.Report[0].Garments);
        Assert.Equal("?period=30days&store_id=1", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GetWorkshopBillingListAsync_ParsesBillsAndLookups()
    {
        var body = """
        {"success":true,"data":{"can_manage_bills":true,
          "bills":[{"id":5,"bill_number":"WB-0005","store_name":"Main Branch","workshop_name":"Sparkle Wash",
            "period_start":"2026-10-01","period_end":"2026-10-31","total_value":5000,"workshop_share":4000,
            "store_share":1000,"share_percent":80,"status":"Unpaid","invoice_date":"2026-11-01","due_date":"2026-11-08",
            "amount_paid":0,"balance_due":4000}],
          "stores":[{"id":1,"name":"Main Branch"}],
          "workshops":[{"id":2,"workshop_name":"Sparkle Wash","commission_workshop_percent":80}],
          "payment_types":[{"id":1,"name":"Cash"}]}}
        """;
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.GetWorkshopBillingListAsync();

        Assert.True(result.CanManageBills);
        Assert.Single(result.Bills);
        Assert.Equal("WB-0005", result.Bills[0].BillNumber);
        Assert.Equal(4000m, result.Bills[0].BalanceDue);
        Assert.Single(result.Workshops);
        Assert.Single(result.PaymentTypes);
    }

    [Fact]
    public async Task GetWorkshopBillingDetailAsync_ParsesItemsAndPayments()
    {
        var body = """
        {"success":true,"data":{"can_manage_bills":true,
          "bill":{"id":5,"bill_number":"WB-0005","store_name":"Main Branch","workshop_name":"Sparkle Wash",
            "total_value":5000,"workshop_share":4000,"store_share":1000,"status":"Unpaid",
            "amount_paid":0,"balance_due":4000,"gst_mode":"none","grand_total":4000},
          "items":[{"bill_item_id":1,"order_id":10,"order_number":"ST-0010","garment_name":"Shirt",
            "customer_name":"Asha Rao","item_value":50}],
          "payments":[]}}
        """;
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.GetWorkshopBillingDetailAsync(5);

        Assert.Equal("WB-0005", result.Bill.BillNumber);
        Assert.Single(result.Items);
        Assert.Equal("Shirt", result.Items[0].GarmentName);
        Assert.Empty(result.Payments);
    }

    [Fact]
    public async Task GenerateWorkshopBillAsync_SendsPayload_AndParsesResult()
    {
        var body = """{"success":true,"data":{"bill_id":6,"bill_number":"WB-0006"}}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.GenerateWorkshopBillAsync(new GenerateWorkshopBillRequest
        {
            StoreId = "all",
            WorkshopId = 2,
            From = "2026-10-01",
            To = "2026-10-31",
        });

        Assert.Equal("WB-0006", result.BillNumber);
        Assert.Equal("/api/mobile/workshop_billing_generate", handler.LastRequest!.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task GenerateWorkshopBillAsync_NoItems_ThrowsWithServerMessage()
    {
        var body = """{"success":false,"error":{"code":"no_items","message":"No workshop items found for this period."}}""";
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.UnprocessableEntity, body));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GenerateWorkshopBillAsync(new GenerateWorkshopBillRequest { StoreId = "1", WorkshopId = 2, From = "2026-10-01", To = "2026-10-31" }));

        Assert.Equal("no_items", ex.Code);
    }

    [Fact]
    public async Task MarkWorkshopBillPaidAsync_SendsPayload_AndParsesResult()
    {
        var body = """{"success":true,"data":{"message":"Bill marked as Paid."}}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.MarkWorkshopBillPaidAsync(new MarkWorkshopBillPaidRequest { BillId = 5, PaymentTypeId = 1 });

        Assert.Equal("Bill marked as Paid.", result.Message);
        Assert.Equal("/api/mobile/workshop_billing_mark_paid", handler.LastRequest!.RequestUri!.AbsolutePath);
    }
}
