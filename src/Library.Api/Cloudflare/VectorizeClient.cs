using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Library.Api.Cloudflare;

public sealed class VectorizeClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly CloudflareOptions _options;
    private readonly ILogger<VectorizeClient> _logger;

    public VectorizeClient(HttpClient httpClient, IOptions<CloudflareOptions> options, ILogger<VectorizeClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _options.ValidateApiAccess();
        _logger = logger;
    }

    public Task<VectorizeMutationResult> UpsertAsync(
        IReadOnlyList<VectorizeVector> vectors,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync("upsert", new { vectors }, cancellationToken);

    public Task<VectorizeMutationResult> DeleteAsync(
        IReadOnlyList<string> ids,
        CancellationToken cancellationToken = default) =>
        SendMutationAsync("delete_by_ids", new { ids }, cancellationToken);

    public async Task<VectorizeQueryResult> QueryAsync(
        IReadOnlyList<float> vector,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        var root = await SendAsync(
            "query",
            new
            {
                vector,
                topK,
                returnMetadata = true
            },
            cancellationToken);

        var result = root.GetProperty("result");
        var matches = result.TryGetProperty("matches", out var matchesProperty) &&
                      matchesProperty.ValueKind == JsonValueKind.Array
            ? matchesProperty.EnumerateArray().Select(ParseMatch).ToArray()
            : [];
        var count = result.TryGetProperty("count", out var countProperty) && countProperty.ValueKind == JsonValueKind.Number
            ? countProperty.GetInt32()
            : matches.Length;

        return new VectorizeQueryResult(matches, count, root.GetRawText());
    }

    private async Task<VectorizeMutationResult> SendMutationAsync(
        string operation,
        object payload,
        CancellationToken cancellationToken)
    {
        var root = await SendAsync(operation, payload, cancellationToken);
        var result = root.GetProperty("result");
        var mutationId = result.TryGetProperty("mutationId", out var mutationIdProperty)
            ? mutationIdProperty.GetString()
            : null;
        var success = root.TryGetProperty("success", out var successProperty) && successProperty.GetBoolean();

        return new VectorizeMutationResult(mutationId, success, root.GetRawText());
    }

    private async Task<JsonElement> SendAsync(string operation, object payload, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.VectorizeIndexName))
        {
            throw new InvalidOperationException("Cloudflare Vectorize configuration is incomplete. Missing VectorizeIndexName.");
        }

        var requestUri = $"accounts/{_options.AccountId}/vectorize/v2/indexes/{_options.VectorizeIndexName}/{operation}";
        using var request = new HttpRequestMessage(HttpMethod.Post, requestUri)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload, SerializerOptions),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Cloudflare Vectorize request to {Operation} failed with status {StatusCode}. Body: {Body}",
                operation,
                (int)response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"Cloudflare Vectorize request to '{operation}' failed with status {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {responseBody}");
        }

        using var document = JsonDocument.Parse(responseBody);
        if (document.RootElement.TryGetProperty("errors", out var errorsProperty) &&
            errorsProperty.ValueKind == JsonValueKind.Array &&
            errorsProperty.GetArrayLength() > 0)
        {
            throw new InvalidOperationException($"Cloudflare Vectorize API error: {errorsProperty.GetRawText()}");
        }

        return document.RootElement.Clone();
    }

    private static VectorizeQueryMatch ParseMatch(JsonElement matchElement)
    {
        var metadata = matchElement.TryGetProperty("metadata", out var metadataElement) &&
                       metadataElement.ValueKind == JsonValueKind.Object
            ? metadataElement.EnumerateObject().ToDictionary(
                property => property.Name,
                property => ToClrValue(property.Value))
            : null;

        return new VectorizeQueryMatch(
            matchElement.GetProperty("id").GetString() ?? string.Empty,
            matchElement.TryGetProperty("score", out var scoreElement) ? scoreElement.GetSingle() : 0f,
            metadata);
    }

    private static object? ToClrValue(JsonElement element) =>
        element.ValueKind switch
        {
            JsonValueKind.String => element.GetString(),
            JsonValueKind.Number when element.TryGetInt64(out var longValue) => longValue,
            JsonValueKind.Number => element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.Null => null,
            _ => element.GetRawText()
        };
}
