using System.Net;

namespace Udc.Client.Tests;

/// <summary>Minimal stand-in for the backend during tests: returns a fixed status/body for every
/// request and records the last request made, so a test can assert on headers/path too.</summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _status;
    private readonly string _body;

    public HttpRequestMessage? LastRequest { get; private set; }

    /// <summary>The request body read eagerly here, before the caller disposes its (`using var req`)
    /// HttpRequestMessage and the JsonContent inside it - reading LastRequest.Content afterwards
    /// throws ObjectDisposedException.</summary>
    public string? LastRequestBody { get; private set; }

    public FakeHttpMessageHandler(HttpStatusCode status, string body)
    {
        _status = status;
        _body = body;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
        var response = new HttpResponseMessage(_status)
        {
            Content = new StringContent(_body, System.Text.Encoding.UTF8, "application/json"),
        };
        return response;
    }
}
