using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using Library.Api.Cloudflare;

namespace Library.Api.Database;

public sealed class D1Client
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<D1Client> _logger;
    private readonly CloudflareOptions _options;

    public D1Client(HttpClient httpClient, IOptions<CloudflareOptions> options, ILogger<D1Client> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _options.ValidateD1Configuration();
    }

    public async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        IReadOnlyList<object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<T>("query", sql, parameters, cancellationToken);
        return result.Results;
    }

    public async Task<D1ExecutionResult> ExecuteAsync(
        string sql,
        IReadOnlyList<object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var result = await SendAsync<JsonElement>("query", sql, parameters, cancellationToken);

        return new D1ExecutionResult(
            result.Meta?.Changes,
            result.Meta?.RowsRead,
            result.Meta?.RowsWritten,
            result.Meta?.Duration,
            result.Success);
    }

    private async Task<D1QueryEnvelope<T>> SendAsync<T>(
        string operation,
        string sql,
        IReadOnlyList<object?>? parameters,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            throw new ArgumentException("SQL must be provided.", nameof(sql));
        }

        var request = new D1QueryRequest(sql, parameters?.ToArray());
        var requestUri = $"accounts/{_options.AccountId}/d1/database/{_options.D1DatabaseId}/{operation}";
        using var message = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(request, SerializerOptions),
                Encoding.UTF8,
                "application/json")
        };

        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw CreateHttpException(sql, response, responseContent);
        }

        var envelope = JsonSerializer.Deserialize<D1ResponseEnvelope<T>>(responseContent, SerializerOptions)
            ?? throw new InvalidOperationException("Cloudflare D1 returned an empty response payload.");

        if (!envelope.Success || envelope.Errors.Count > 0)
        {
            throw CreateApiException(sql, response, envelope, responseContent);
        }

        var firstResult = envelope.Result.FirstOrDefault()
            ?? throw new InvalidOperationException("Cloudflare D1 returned no result objects.");

        if (!firstResult.Success)
        {
            throw CreateStatementException(sql, response, firstResult, responseContent);
        }

        return new D1QueryEnvelope<T>(firstResult.Results, firstResult.Meta, firstResult.Success);
    }

    private Exception CreateHttpException(string sql, HttpResponseMessage response, string responseContent)
    {
        _logger.LogError(
            "Cloudflare D1 HTTP failure for SQL [{Sql}]. Status: {StatusCode}. Body: {Body}",
            sql,
            (int)response.StatusCode,
            responseContent);

        return new HttpRequestException(
            $"Cloudflare D1 request failed with status {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {responseContent}");
    }

    private Exception CreateApiException<T>(
        string sql,
        HttpResponseMessage response,
        D1ResponseEnvelope<T> envelope,
        string responseContent)
    {
        var errorMessage = envelope.Errors.Count > 0
            ? string.Join("; ", envelope.Errors.Select(error => $"{error.Code}: {error.Message}"))
            : "Unknown Cloudflare D1 API error.";

        _logger.LogError(
            "Cloudflare D1 API failure for SQL [{Sql}]. Status: {StatusCode}. Errors: {Errors}. Body: {Body}",
            sql,
            (int)response.StatusCode,
            errorMessage,
            responseContent);

        return new InvalidOperationException($"Cloudflare D1 API error: {errorMessage}");
    }

    private Exception CreateStatementException<T>(
        string sql,
        HttpResponseMessage response,
        D1Result<T> statementResult,
        string responseContent)
    {
        var errorMessage = statementResult.Error ?? "Unknown statement execution error.";

        _logger.LogError(
            "Cloudflare D1 statement failure for SQL [{Sql}]. Status: {StatusCode}. Error: {Error}. Body: {Body}",
            sql,
            (int)response.StatusCode,
            errorMessage,
            responseContent);

        return new InvalidOperationException($"Cloudflare D1 statement error: {errorMessage}");
    }

    private sealed record D1QueryRequest(string Sql, object?[]? Params);
}
