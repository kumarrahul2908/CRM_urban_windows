using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Udc.Client.Models;

namespace Udc.Client;

/// <summary>
/// Thin wrapper over the backend's existing <c>/api/mobile/*</c> REST API - the same API the
/// Android app uses. All business logic stays server-side; this just sends requests and unwraps
/// the <c>{"success":true,"data":...}</c> / <c>{"success":false,"error":{...}}</c> envelope.
/// </summary>
public sealed class ApiClient : IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;

    public string? BearerToken { get; set; }

    public ApiClient(string baseUrl) : this(baseUrl, new HttpClientHandler())
    {
    }

    /// <summary>Test hook: inject a fake <see cref="HttpMessageHandler"/> instead of hitting the network.</summary>
    public ApiClient(string baseUrl, HttpMessageHandler handler)
    {
        _http = new HttpClient(handler) { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(20) };
    }

    private void ApplyAuth(HttpRequestMessage req)
    {
        if (!string.IsNullOrEmpty(BearerToken))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
    }

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        using var req = new HttpRequestMessage(method, path);
        ApplyAuth(req);
        if (body is not null) req.Content = JsonContent.Create(body, options: JsonOptions);

        HttpResponseMessage resp;
        try
        {
            resp = await _http.SendAsync(req, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw new ApiException("network_error", "Could not reach the local server. Is it still running?", ex);
        }

        var json = await resp.Content.ReadAsStringAsync(ct);
        ApiEnvelope<T>? envelope;
        try
        {
            envelope = JsonSerializer.Deserialize<ApiEnvelope<T>>(json, JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new ApiException("bad_response", $"Unexpected response from server (HTTP {(int)resp.StatusCode}).", ex);
        }

        if (envelope is null)
            throw new ApiException("bad_response", $"Empty response from server (HTTP {(int)resp.StatusCode}).");
        if (!envelope.Success)
            throw new ApiException(envelope.Error?.Code ?? "unknown_error", envelope.Error?.Message ?? "Unknown error.");
        if (resp.StatusCode == HttpStatusCode.NoContent || envelope.Data is null)
            return default!;
        return envelope.Data;
    }

    public Task<LoginResult> LoginAsync(string username, string password, string deviceName, CancellationToken ct = default) =>
        SendAsync<LoginResult>(HttpMethod.Post, "/api/mobile/login", new
        {
            username,
            password,
            device_name = deviceName,
            device_id = Environment.MachineName,
        }, ct);

    public Task LogoutAsync(CancellationToken ct = default) =>
        SendAsync<object>(HttpMethod.Post, "/api/mobile/logout", null, ct);

    public Task<MeResult> GetMeAsync(CancellationToken ct = default) =>
        SendAsync<MeResult>(HttpMethod.Get, "/api/mobile/me", null, ct);

    public Task<DashboardSummary> GetDashboardSummaryAsync(CancellationToken ct = default) =>
        SendAsync<DashboardSummary>(HttpMethod.Get, "/api/mobile/dashboard_summary", null, ct);

    private static string BuildQuery(params (string Key, string? Value)[] parms)
    {
        var parts = parms.Where(p => !string.IsNullOrEmpty(p.Value)).Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}");
        var qs = string.Join("&", parts);
        return qs.Length > 0 ? "?" + qs : "";
    }

    // ---- customers ---------------------------------------------------------------------------

    public Task<CustomerListResult> GetCustomersAsync(string search = "", int page = 1, CancellationToken ct = default) =>
        SendAsync<CustomerListResult>(HttpMethod.Get, "/api/mobile/customers_list" + BuildQuery(("q", search), ("page", page.ToString())), null, ct);

    public Task<CustomerDetailResult> GetCustomerDetailAsync(int id, CancellationToken ct = default) =>
        SendAsync<CustomerDetailResult>(HttpMethod.Get, "/api/mobile/customers_detail" + BuildQuery(("id", id.ToString())), null, ct);

    public Task<CreateCustomerResult> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken ct = default) =>
        SendAsync<CreateCustomerResult>(HttpMethod.Post, "/api/mobile/customers_create", request, ct);

    // ---- orders --------------------------------------------------------------------------------

    public Task<OrderListResult> GetOrdersAsync(string search = "", int page = 1, CancellationToken ct = default) =>
        SendAsync<OrderListResult>(HttpMethod.Get, "/api/mobile/orders_list" + BuildQuery(("q", search), ("page", page.ToString())), null, ct);

    public Task<OrderDetailResult> GetOrderDetailAsync(int id, CancellationToken ct = default) =>
        SendAsync<OrderDetailResult>(HttpMethod.Get, "/api/mobile/orders_detail" + BuildQuery(("id", id.ToString())), null, ct);

    public Task<PaymentTypesResult> GetPaymentTypesAsync(CancellationToken ct = default) =>
        SendAsync<PaymentTypesResult>(HttpMethod.Get, "/api/mobile/payments_list", null, ct);

    public Task<RecordPaymentResult> RecordPaymentAsync(RecordPaymentRequest request, CancellationToken ct = default) =>
        SendAsync<RecordPaymentResult>(HttpMethod.Post, "/api/mobile/orders_record_payment", request, ct);

    // ---- POS / order creation -----------------------------------------------------------------

    public Task<ProductsResult> GetProductsAsync(int customerId, string search = "", int perPage = 300, CancellationToken ct = default) =>
        SendAsync<ProductsResult>(HttpMethod.Get, "/api/mobile/products" + BuildQuery(("customer_id", customerId.ToString()), ("q", search), ("per_page", perPage.ToString())), null, ct);

    public Task<PosCustomerSummary> GetPosCustomerSummaryAsync(int customerId, CancellationToken ct = default) =>
        SendAsync<PosCustomerSummary>(HttpMethod.Get, "/api/mobile/pos_customer_summary" + BuildQuery(("customer_id", customerId.ToString())), null, ct);

    public async Task<List<UrgentLevel>> GetUrgentLevelsAsync(CancellationToken ct = default) =>
        (await SendAsync<UrgentLevelsResult>(HttpMethod.Get, "/api/mobile/urgent_levels", null, ct)).UrgentLevels;

    public Task<PosAvailableStoresResult> GetAvailableStoresAsync(CancellationToken ct = default) =>
        SendAsync<PosAvailableStoresResult>(HttpMethod.Get, "/api/mobile/pos_available_stores", null, ct);

    public Task<CreateOrderResult> CreateOrderAsync(CreateOrderRequest request, CancellationToken ct = default) =>
        SendAsync<CreateOrderResult>(HttpMethod.Post, "/api/mobile/pos_orders_create", request, ct);

    // ---- workshop -------------------------------------------------------------------------------

    public Task<WorkshopQueueResult> GetWorkshopQueueAsync(string order = "", string status = "", int page = 1, CancellationToken ct = default) =>
        SendAsync<WorkshopQueueResult>(HttpMethod.Get, "/api/mobile/workshop_queue" + BuildQuery(("order", order), ("status", status), ("page", page.ToString())), null, ct);

    public Task<WorkshopActionResult> SendWorkshopItemActionAsync(WorkshopItemActionRequest request, CancellationToken ct = default) =>
        SendAsync<WorkshopActionResult>(HttpMethod.Post, "/api/mobile/workshop_action", request, ct);

    public Task<WorkshopActionResult> CreateChallanAsync(CreateChallanRequest request, CancellationToken ct = default) =>
        SendAsync<WorkshopActionResult>(HttpMethod.Post, "/api/mobile/workshop_action", request, ct);

    // ---- barcode --------------------------------------------------------------------------------

    public Task<BarcodeLookupResult> LookupBarcodeAsync(string barcode, CancellationToken ct = default) =>
        SendAsync<BarcodeLookupResult>(HttpMethod.Get, "/api/mobile/barcode_lookup" + BuildQuery(("barcode", barcode)), null, ct);

    // ---- reports --------------------------------------------------------------------------------

    public Task<ReportsSummaryResult> GetReportsSummaryAsync(string dateFrom = "", string dateTo = "", CancellationToken ct = default) =>
        SendAsync<ReportsSummaryResult>(HttpMethod.Get, "/api/mobile/reports_summary" + BuildQuery(("date_from", dateFrom), ("date_to", dateTo)), null, ct);

    public Task<OutstandingListResult> GetOutstandingListAsync(string customer = "", CancellationToken ct = default) =>
        SendAsync<OutstandingListResult>(HttpMethod.Get, "/api/mobile/outstanding_list" + BuildQuery(("customer", customer)), null, ct);

    public Task<DailyCollectionResult> GetDailyCollectionAsync(string date = "", CancellationToken ct = default) =>
        SendAsync<DailyCollectionResult>(HttpMethod.Get, "/api/mobile/daily_collection" + BuildQuery(("date", date)), null, ct);

    public void Dispose() => _http.Dispose();
}
