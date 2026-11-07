using System.Net;
using Udc.Client;
using Udc.Client.Models;
using Xunit;

namespace Udc.Client.Tests;

public class PosApiClientTests
{
    [Fact]
    public async Task GetProductsAsync_ParsesGarmentsWithRates()
    {
        var body = """
        {"success":true,"data":{
          "price_list":{"id":3,"name":"Default"},
          "garments":[{"id":10,"name":"Shirt","sku":"SH1","category_name":"Formal","default_service_id":1,
            "rates":[{"service_id":1,"service_name":"Wash & Iron","billing_unit":"piece","piece_rate":40,"weight_rate":null,"tax_percent":0}]}],
          "universal_services":[],"total":1}}
        """;
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.GetProductsAsync(customerId: 1, search: "shirt");

        Assert.Equal("Default", result.PriceList!.Name);
        Assert.Single(result.Garments);
        Assert.Single(result.Garments[0].Rates);
        Assert.Equal(40m, result.Garments[0].Rates[0].PieceRate);
        Assert.Equal("/api/mobile/products", handler.LastRequest!.RequestUri!.AbsolutePath);
        Assert.Equal("?customer_id=1&q=shirt&per_page=300", handler.LastRequest.RequestUri!.Query);
    }

    [Fact]
    public async Task GetPosCustomerSummaryAsync_ParsesCustomerAndPriceList()
    {
        var body = """
        {"success":true,"data":{"customer":{"id":1,"name":"Asha Rao","mobile":"9000000000","home_delivery":true,"discount_percent":5},
          "price_list_name":"Default","outstanding":120,"wallet_balance":50}}
        """;
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.GetPosCustomerSummaryAsync(1);

        Assert.Equal("Asha Rao", result.Customer.Name);
        Assert.True(result.Customer.HomeDelivery);
        Assert.Equal(120m, result.Outstanding);
    }

    [Fact]
    public async Task GetUrgentLevelsAsync_UnwrapsTheList()
    {
        var body = """{"success":true,"data":{"urgent_levels":[{"id":1,"level_name":"Express","surcharge_percent":20}]}}""";
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var levels = await client.GetUrgentLevelsAsync();

        Assert.Single(levels);
        Assert.Equal("Express", levels[0].LevelName);
    }

    [Fact]
    public async Task GetAvailableStoresAsync_ParsesIsFixed()
    {
        var body = """{"success":true,"data":{"stores":[{"id":1,"store_name":"Main Branch"}],"is_fixed":true}}""";
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.GetAvailableStoresAsync();

        Assert.True(result.IsFixed);
        Assert.Equal("Main Branch", result.Stores[0].StoreName);
    }

    [Fact]
    public async Task CreateOrderAsync_SendsItemsAndParsesResult()
    {
        var body = """{"success":true,"data":{"order_id":77,"order_number":"ST-0077","invoice_number":"INV-0077","grand_total":80,"warnings":[]}}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.CreateOrderAsync(new CreateOrderRequest
        {
            ClientRequestId = "test-order-1",
            CustomerId = 1,
            Items = new List<CreateOrderItemLine> { new() { GarmentId = 10, ServiceId = 1, Quantity = 2 } },
        });

        Assert.Equal(77, result.OrderId);
        Assert.Equal("ST-0077", result.OrderNumber);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/mobile/pos_orders_create", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CreateOrderAsync_ValidationFailure_ThrowsWithServerMessage()
    {
        var body = """{"success":false,"error":{"code":"validation_failed","message":"Add at least one garment to the order."}}""";
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.UnprocessableEntity, body));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.CreateOrderAsync(new CreateOrderRequest { CustomerId = 1 }));

        Assert.Equal("validation_failed", ex.Code);
        Assert.Contains("at least one garment", ex.Message);
    }

    [Fact]
    public async Task CreateOrderAsync_SendsBrandColorRemarksOverrideAndPayments()
    {
        var body = """{"success":true,"data":{"order_id":78,"order_number":"ST-0078","invoice_number":"INV-0078","grand_total":120,"warnings":[]}}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var request = new CreateOrderRequest
        {
            ClientRequestId = "test-order-2",
            CustomerId = 1,
            Items = new List<CreateOrderItemLine>
            {
                new() { GarmentId = 10, ServiceId = 1, Quantity = 1, BrandId = 3, ColorId = 5, Remarks = "Small stain on collar", ManualOverride = true, RateOverride = 100 },
            },
            Payments = new List<PaymentLine> { new() { PaymentTypeId = 1, Amount = 60 } },
            WalletAmount = 60,
        };

        var result = await client.CreateOrderAsync(request);

        Assert.Equal(78, result.OrderId);
        Assert.Single(request.Items);
        Assert.True(request.Items[0].ManualOverride);
        Assert.Equal(100m, request.Items[0].RateOverride);
        Assert.Single(request.Payments);
        Assert.Equal(60m, request.WalletAmount);
    }

    [Fact]
    public async Task GetBrandsAsync_UnwrapsTheList()
    {
        var body = """{"success":true,"data":{"brands":[{"id":3,"brand_name":"Raymond"}]}}""";
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var brands = await client.GetBrandsAsync();

        Assert.Single(brands);
        Assert.Equal("Raymond", brands[0].BrandName);
    }

    [Fact]
    public async Task GetColorsAsync_UnwrapsTheList()
    {
        var body = """{"success":true,"data":{"colors":[{"id":5,"color_name":"Blue"}]}}""";
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var colors = await client.GetColorsAsync();

        Assert.Single(colors);
        Assert.Equal("Blue", colors[0].ColorName);
    }

    [Fact]
    public async Task GetProductsAsync_ParsesUniversalServices()
    {
        var body = """
        {"success":true,"data":{
          "price_list":{"id":3,"name":"Default"},
          "garments":[{"id":10,"name":"Shirt","sku":"SH1","category_name":"Formal","default_service_id":1,"rates":[]}],
          "universal_services":[{"service_id":5,"service_name":"Wash & Fold","billing_unit":"weight","piece_rate":null,"weight_rate":60,"tax_percent":0}],
          "total":1}}
        """;
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var result = await client.GetProductsAsync(customerId: 1);

        Assert.Single(result.UniversalServices);
        Assert.Equal("Wash & Fold", result.UniversalServices[0].ServiceName);
        Assert.Equal(60m, result.UniversalServices[0].WeightRate);
    }

    [Fact]
    public async Task CreateLaundryOrderAsync_SendsBatchesAndParsesResult()
    {
        var body = """{"success":true,"data":{"order_id":90,"order_number":"ST-0090","grand_total":300,"warnings":[]}}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        var result = await client.CreateLaundryOrderAsync(new CreateLaundryOrderRequest
        {
            ClientRequestId = "test-laundry-1",
            CustomerId = 1,
            Batches = new List<CreateLaundryBatchLine>
            {
                new()
                {
                    ServiceId = 5, Weight = 5, Rate = 60, GarmentCount = 2,
                    Garments = new List<LaundryBatchGarmentLine> { new() { GarmentId = 10, Quantity = 2 } },
                },
            },
        });

        Assert.Equal(90, result.OrderId);
        Assert.Equal("ST-0090", result.OrderNumber);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("/api/mobile/laundry_orders_create", handler.LastRequest.RequestUri!.AbsolutePath);
    }

    [Fact]
    public async Task CreateLaundryOrderAsync_SendsStoreIdForFloatingStoreAccounts()
    {
        var body = """{"success":true,"data":{"order_id":92,"order_number":"ST-0092","grand_total":300,"warnings":[]}}""";
        var handler = new FakeHttpMessageHandler(HttpStatusCode.OK, body);
        using var client = new ApiClient("http://127.0.0.1:3100", handler);

        await client.CreateLaundryOrderAsync(new CreateLaundryOrderRequest
        {
            ClientRequestId = "test-laundry-3",
            CustomerId = 1,
            StoreId = 2,
            Batches = new List<CreateLaundryBatchLine>
            {
                new() { ServiceId = 5, Weight = 5, Rate = 60, GarmentCount = 1, Garments = new List<LaundryBatchGarmentLine> { new() { GarmentId = 10, Quantity = 1 } } },
            },
        });

        Assert.Contains("\"store_id\":2", handler.LastRequestBody);
    }

    [Fact]
    public async Task CreateLaundryOrderAsync_SendsPaymentsAndWalletAmount()
    {
        var body = """{"success":true,"data":{"order_id":91,"order_number":"ST-0091","grand_total":300,"warnings":[]}}""";
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.OK, body));

        var request = new CreateLaundryOrderRequest
        {
            ClientRequestId = "test-laundry-2",
            CustomerId = 1,
            Batches = new List<CreateLaundryBatchLine>
            {
                new() { ServiceId = 5, Weight = 5, Rate = 60, GarmentCount = 1, Garments = new List<LaundryBatchGarmentLine> { new() { GarmentId = 10, Quantity = 1 } } },
            },
            Payments = new List<PaymentLine> { new() { PaymentTypeId = 1, Amount = 150 } },
            WalletAmount = 50,
        };

        var result = await client.CreateLaundryOrderAsync(request);

        Assert.Equal(91, result.OrderId);
        Assert.Single(request.Payments);
        Assert.Equal(50m, request.WalletAmount);
    }

    [Fact]
    public async Task CreateLaundryOrderAsync_ValidationFailure_ThrowsWithServerMessage()
    {
        var body = """{"success":false,"error":{"code":"validation_failed","message":"Add at least one Laundry batch to the order."}}""";
        using var client = new ApiClient("http://127.0.0.1:3100", new FakeHttpMessageHandler(HttpStatusCode.UnprocessableEntity, body));

        var ex = await Assert.ThrowsAsync<ApiException>(() => client.CreateLaundryOrderAsync(new CreateLaundryOrderRequest { CustomerId = 1 }));

        Assert.Equal("validation_failed", ex.Code);
        Assert.Contains("at least one Laundry batch", ex.Message);
    }
}
