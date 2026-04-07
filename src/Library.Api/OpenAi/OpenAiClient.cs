using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace Library.Api.OpenAi;

public sealed class OpenAiClient
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly HttpClient _httpClient;
    private readonly OpenAiOptions _options;
    private readonly ILogger<OpenAiClient> _logger;

    public OpenAiClient(HttpClient httpClient, IOptions<OpenAiOptions> options, ILogger<OpenAiClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _options.ValidateApiKey();
        _logger = logger;
    }

    public Task<OpenAiResponseResult> CreateAssessmentResponseAsync(
        string input,
        string? instructions = null,
        int? maxOutputTokens = null,
        CancellationToken cancellationToken = default) =>
        CreateResponseAsync(_options.AssessmentModel, input, instructions, maxOutputTokens, cancellationToken);

    public Task<OpenAiResponseResult> CreateChatResponseAsync(
        string input,
        string? instructions = null,
        int? maxOutputTokens = null,
        CancellationToken cancellationToken = default) =>
        CreateResponseAsync(_options.ChatModel, input, instructions, maxOutputTokens, cancellationToken);

    public async Task<OpenAiEmbeddingResult> CreateEmbeddingAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model = _options.EmbeddingModel,
            input,
            encoding_format = "float"
        };

        var root = await SendAsync("embeddings", payload, cancellationToken);
        var data = root.GetProperty("data");
        if (data.GetArrayLength() == 0)
        {
            throw new InvalidOperationException("OpenAI embeddings response did not include any embedding data.");
        }

        var firstEmbedding = data[0].GetProperty("embedding")
            .EnumerateArray()
            .Select(value => value.GetSingle())
            .ToArray();

        var model = root.GetProperty("model").GetString() ?? _options.EmbeddingModel;

        return new OpenAiEmbeddingResult(model, firstEmbedding, root.GetRawText());
    }

    public async Task<OpenAiResponseResult> CreateResponseAsync(
        string model,
        string input,
        string? instructions = null,
        int? maxOutputTokens = null,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            model,
            input,
            instructions,
            max_output_tokens = maxOutputTokens
        };

        var root = await SendAsync("responses", payload, cancellationToken);
        if (!root.TryGetProperty("id", out var idProperty) || idProperty.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException("OpenAI response payload did not include a string id.");
        }

        var id = idProperty.GetString()
            ?? throw new InvalidOperationException("OpenAI response payload did not include an id.");
        var status = root.TryGetProperty("status", out var statusProperty)
            && statusProperty.ValueKind == JsonValueKind.String
            ? statusProperty.GetString() ?? "unknown"
            : "unknown";
        var outputText = ExtractOutputText(root);

        return new OpenAiResponseResult(
            id,
            root.TryGetProperty("model", out var modelProperty) && modelProperty.ValueKind == JsonValueKind.String
                ? modelProperty.GetString() ?? model
                : model,
            status,
            outputText,
            root.GetRawText());
    }

    private async Task<JsonElement> SendAsync(string path, object payload, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(payload, SerializerOptions),
                Encoding.UTF8,
                "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ApiKey);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "OpenAI request to {Path} failed with status {StatusCode}. Body: {Body}",
                path,
                (int)response.StatusCode,
                responseBody);

            throw new HttpRequestException(
                $"OpenAI request to '{path}' failed with status {(int)response.StatusCode} ({response.ReasonPhrase}). Body: {responseBody}");
        }

        using var document = JsonDocument.Parse(responseBody);

        if (document.RootElement.TryGetProperty("error", out var errorElement) &&
            errorElement.ValueKind is not JsonValueKind.Null and not JsonValueKind.Undefined)
        {
            var message = errorElement.ValueKind == JsonValueKind.Object &&
                          errorElement.TryGetProperty("message", out var messageElement) &&
                          messageElement.ValueKind == JsonValueKind.String
                ? messageElement.GetString()
                : errorElement.GetRawText();

            _logger.LogError("OpenAI API returned an error payload for {Path}: {Error}", path, message);
            throw new InvalidOperationException($"OpenAI API error: {message}");
        }

        return document.RootElement.Clone();
    }

    private static string ExtractOutputText(JsonElement root)
    {
        if (root.TryGetProperty("output_text", out var outputTextProperty) &&
            outputTextProperty.ValueKind == JsonValueKind.String)
        {
            return outputTextProperty.GetString() ?? string.Empty;
        }

        if (!root.TryGetProperty("output", out var outputProperty) ||
            outputProperty.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var parts = new List<string>();

        foreach (var outputItem in outputProperty.EnumerateArray())
        {
            if (outputItem.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!outputItem.TryGetProperty("content", out var contentProperty) ||
                contentProperty.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var contentItem in contentProperty.EnumerateArray())
            {
                if (contentItem.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                if (contentItem.TryGetProperty("text", out var textProperty) &&
                    textProperty.ValueKind == JsonValueKind.String)
                {
                    parts.Add(textProperty.GetString() ?? string.Empty);
                }
            }
        }

        return string.Concat(parts);
    }
}
