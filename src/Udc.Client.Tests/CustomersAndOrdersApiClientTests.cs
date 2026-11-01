using System.Net;
using Udc.Client;
using Udc.Client.Models;
using Xunit;

namespace Udc.Client.Tests;

public class CustomersAndOrdersApiClientTests
{
    [Fact]
    public async Task GetCustomersAsync_BuildsQueryString_AndParsesResults()
    {
        var body = """
        {"success":true,"data":{"results":[{"id":1,"customer_code":"C0001","title":"Mr","full_name":"Asha Rao",
          "mobile":"9000000000","area":"Kothrud","price_list_name":"Default","discount_percent":5,
          "home_delivery":true,"is_active":true}],"page":1,"per_page":20,"total":1,"total_pages":1}}
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.GetCustomersAsync("asha", 2);

        Assert.Single(result.Results);
        Assert.Equal("Asha Rao", result.Results[0].FullName);
        Assert.Equal("/api/mobile/customers_list", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("?q=asha&page=2", handler.LastRequest.RequestUri!.Query);
    }

    [Fact]
    public async Task GetCustomersAsync_OmitsBlankSearch_FromQueryString()
    {
        var body = """{"success":true,"data":{"results":[],"page":1,"per_page":20,"total":0,"total_pages":0}}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        await client.GetCustomersAsync("", 1);

        Assert.Equal("?page=1", handler.LastRequest!.RequestUri!.Query);
    }

    [Fact]
    public async Task GetCustomerDetailAsync_ParsesWalletAndOutstanding()
    {
        var body = """
        {"success":true,"data":{"customer":{"id":7,"customer_code":"C0007","full_name":"Ravi Kumar",
          "mobile":"9111111111","wallet_balance":150.5,"outstanding_balance":320,"is_active":true,
          "home_delivery":false}}}
        """;
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.GetCustomerDetailAsync(7);

        Assert.Equal("Ravi Kumar", result.Customer.FullName);
        Assert.Equal(150.5m, result.Customer.WalletBalance);
        Assert.Equal(320m, result.Customer.OutstandingBalance);
    }

    [Fact]
    public async Task CreateCustomerAsync_DuplicateMobile_ThrowsWithServerMessage()
    {
        var body = """
        {"success":false,"error":{"code":"duplicate_customer","message":"This mobile number is already registered for Asha Rao (C0001). Please use the existing customer instead of creating a duplicate."}}
        """;
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.Conflict, body));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.CreateCustomerAsync(new CreateCustomerRequest { FullName = "Asha R", Mobile = "9000000000" }));

        Assert.Equal("duplicate_customer", ex.Code);
        Assert.Contains("Asha Rao", ex.Message);
    }

    [Fact]
    public async Task GetOrdersAsync_ParsesListRows()
    {
        var body = """
        {"success":true,"data":{"results":[{"id":55,"order_number":"ST-0055","order_mode":"piece",
          "grand_total":1200,"total_paid":500,"current_due":700,"customer_name":"Asha Rao",
          "status_label":"Ready","status_key":"ready","color":"#0a0"}],"page":1,"per_page":20,"total":1,"total_pages":1}}
        """;
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.GetOrdersAsync();

        Assert.Single(result.Results);
        Assert.Equal("ST-0055", result.Results[0].OrderNumber);
        Assert.Equal(700m, result.Results[0].CurrentDue);
    }

    [Fact]
    public async Task GetOrderDetailAsync_ParsesItemsAndPayments()
    {
        var body = """
        {"success":true,"data":{
          "order":{"id":55,"order_number":"ST-0055","customer_id":1,"order_mode":"piece","grand_total":1200,
                    "total_paid":500,"current_due":700,"status_key":"ready","status_label":"Ready"},
          "order_actions":{"can_edit":false,"can_add_item":false,"can_remove_item":false,"can_cancel":false},
          "receivable_status":"Open",
          "items":[{"id":1,"garment_name":"Shirt","service_name":"Wash & Iron","quantity":2,"unit_price":50,
                    "subtotal":100,"total":100,"defects":["Button missing"]}],
          "laundry_garments":[],
          "payments":[{"id":9,"payment_number":"PMT-0009","total_amount":500,"payment_kind":"Partial","breakdown":"Cash: 500"}],
          "wallet_balance":0}}
        """;
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.GetOrderDetailAsync(55);

        Assert.Equal("ST-0055", result.Order.OrderNumber);
        Assert.Single(result.Items);
        Assert.Equal("Shirt", result.Items[0].GarmentName);
        Assert.Contains("Button missing", result.Items[0].Defects);
        Assert.Single(result.Payments);
        Assert.Equal("Cash: 500", result.Payments[0].Breakdown);
    }

    [Fact]
    public async Task GetPaymentTypesAsync_ParsesTypeList()
    {
        var body = """{"success":true,"data":{"payments":[],"payment_types":[{"id":1,"type_name":"Cash"},{"id":2,"type_name":"UPI"}],"page":1,"per_page":30,"total":0}}""";
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.GetPaymentTypesAsync();

        Assert.Equal(2, result.PaymentTypes.Count);
        Assert.Equal("Cash", result.PaymentTypes[0].TypeName);
    }

    [Fact]
    public async Task RecordPaymentAsync_SendsPayload_AndParsesUpdatedDue()
    {
        var body = """{"success":true,"data":{"payment_id":10,"payment_number":"PMT-0010","updated_due":200,"idempotent_replay":false}}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.RecordPaymentAsync(new RecordPaymentRequest
        {
            OrderId = 55,
            ClientRequestId = "test-req-1",
            PaymentLines = new List<PaymentLine> { new() { PaymentTypeId = 1, Amount = 500 } },
        });

        Assert.Equal(200m, result.UpdatedDue);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/mobile/orders_record_payment", handler.LastRequest.RequestUri!.AbsolutePath);
    }
}
