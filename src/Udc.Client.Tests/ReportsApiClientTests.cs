using System.Net;
using Udc.Client;
using Udc.Client.Models;
using Xunit;

namespace Udc.Client.Tests;

public class ReportsApiClientTests
{
    [Fact]
    public async Task GetReportsSummaryAsync_ParsesAllFourSections()
    {
        var body = """
        {"success":true,"data":{
          "date_from":"2026-11-01","date_to":"2026-11-01",
          "sales":{"order_count":5,"order_value":5000,"avg_order_value":1000,
            "by_status":[{"status_label":"Ready","color":"#0a0","count":2}],
            "by_service":[{"service_name":"Wash & Iron","billing_unit":"piece","item_count":10,"revenue":2000}],
            "top_garments":[{"garment_name":"Shirt","item_count":6,"revenue":600}]},
          "payments":{"collections":[{"type_name":"Cash","total":3000}],"refunds":[],
            "total_collected":3000,"total_refunded":0,"advance_deposits":500,
            "open_receivables":{"paid":2,"partial":1,"unpaid":1}},
          "customers":{"new_customers":3,"returning_customers":2,
            "top_customers":[{"full_name":"Asha Rao","mobile":"9000000000","order_count":2,"revenue":1800,"outstanding":200}]},
          "operations":{"in_workshop":4,"processing":2,"qc_pending":1,"in_rework":0,"ready_to_pack":1,"packed":1,"qc_passed":5,"qc_failed":1,"overdue_orders":2,"urgent_orders":1}}}
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.GetReportsSummaryAsync("2026-11-01", "2026-11-01");

        Assert.Equal(5, result.Sales.OrderCount);
        Assert.Single(result.Sales.ByStatus);
        Assert.Equal(3000m, result.Payments.TotalCollected);
        Assert.Equal(2, result.Payments.OpenReceivables.Paid);
        Assert.Single(result.Customers.TopCustomers);
        Assert.Equal(4, result.Operations.InWorkshop);
        Assert.Equal("?date_from=2026-11-01&date_to=2026-11-01", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GetOutstandingListAsync_ParsesReceivablesAndBucketTotals()
    {
        var body = """
        {"success":true,"data":{
          "receivables":[{"order_id":10,"order_number":"ST-0010","customer_name":"Asha Rao","mobile":"9000000000",
            "total_amount":1200,"paid_amount":500,"due_amount":700,"due_date":"2026-11-05","ageing_bucket":"Current"}],
          "bucket_totals":{"Current":700,"1-7 days":0,"8-30 days":0,"31-60 days":0,"61-90 days":0,"90+ days":0},
          "grand_outstanding":700,
          "by_customer":[{"name":"Asha Rao","mobile":"9000000000","orders":1,"total_due":700}]}}
        """;
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.GetOutstandingListAsync("Asha");

        Assert.Single(result.Receivables);
        Assert.Equal(700m, result.Receivables[0].DueAmount);
        Assert.Equal(700m, result.BucketTotals["Current"]);
        Assert.Equal(700m, result.GrandOutstanding);
        Assert.Single(result.ByCustomer);
    }

    [Fact]
    public async Task GetDailyCollectionAsync_ParsesTotalsAndDetails()
    {
        var body = """
        {"success":true,"data":{"date":"2026-11-01",
          "collections":[{"type_name":"Cash","total":900}],"refunds":[],
          "total_collections":900,"total_refunds":0,"net_collection":900,"advance_received":100,
          "details":[{"payment_number":"PMT-0001","payment_date":"2026-11-01 10:00:00","payment_kind":"Full",
            "total_amount":900,"customer_name":"Asha Rao","order_number":"ST-0010","created_by_name":"Admin","breakdown":"Cash: 900"}]}}
        """;
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.GetDailyCollectionAsync("2026-11-01");

        Assert.Equal(900m, result.TotalCollections);
        Assert.Equal(100m, result.AdvanceReceived);
        Assert.Single(result.Details);
        Assert.Equal("Cash: 900", result.Details[0].Breakdown);
    }

    [Fact]
    public async Task GetAuditLogsAsync_ParsesEntriesAndOptions()
    {
        var body = """
        {"success":true,"data":{
          "entries":[{"id":1,"created_at":"2026-11-01 10:00:00","actor":"Admin","module":"orders","action":"create",
            "record_id":"55","old_value":null,"new_value":"{\"order_number\":\"ST-0055\"}","ip_address":"[REDACTED]"}],
          "page":1,"per_page":25,"total":1,
          "options":{"modules":["orders","customers"],"actions":["create","edit"]}}}
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.GetAuditLogsAsync(module: "orders");

        Assert.Single(result.Entries);
        Assert.Equal("Admin", result.Entries[0].Actor);
        Assert.NotNull(result.Options);
        Assert.Contains("customers", result.Options!.Modules);
        Assert.Equal("?module=orders&page=1&options=1", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GetAuditLogsAsync_Forbidden_ThrowsWithServerMessage()
    {
        var body = """{"success":false,"error":{"code":"forbidden","message":"Only Super Admin and Admin can view the audit log."}}""";
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.Forbidden, body));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.GetAuditLogsAsync());

        Assert.Equal("forbidden", ex.Code);
    }

    [Fact]
    public async Task GetDormantCustomersAsync_ParsesCustomersAndThresholds()
    {
        var body = """
        {"success":true,"data":{
          "customers":[{"id":3,"full_name":"Old Customer","mobile":"9222222222","communication_preference":"SMS",
            "last_order_date":"2026-08-01","days_inactive":92}],
          "min_days":30,"max_days":null}}
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.GetDormantCustomersAsync(60);

        Assert.Single(result.Customers);
        Assert.Equal(92, result.Customers[0].DaysInactive);
        Assert.Equal(30, result.MinDays);
        Assert.Equal("?min_days=60", handler.LastRequest!.RequestUri!.Query);
    }
}
