using System.Text.Json;
using Microsoft.Extensions.Options;
using Library.Api.OpenAi;

namespace Library.Api.Assessment;

public sealed class AiAssessmentService
{
    private readonly OpenAiClient _openAiClient;
    private readonly AiAssessmentOptions _options;

    public AiAssessmentService(OpenAiClient openAiClient, IOptions<AiAssessmentOptions> options)
    {
        _openAiClient = openAiClient;
        _options = options.Value;
    }

    public async Task<AiAssessmentResult> AssessAsync(
        string markdown,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return new AiAssessmentResult(
                false,
                null,
                null,
                [],
                null,
                "Markdown content is required for assessment.",
                null);
        }

        var truncatedMarkdown = Truncate(markdown, _options.MaxInputCharacters);
        var prompt = BuildPrompt(title, truncatedMarkdown);

        OpenAiResponseResult response;
        try
        {
            response = await _openAiClient.CreateAssessmentResponseAsync(
                prompt,
                "Return only valid JSON with fields systemRating, summary, tags, and reasoning.",
                _options.MaxOutputTokens,
                cancellationToken);
        }
        catch (Exception exception)
        {
            return new AiAssessmentResult(false, null, null, [], null, exception.Message, null);
        }

        try
        {
            var json = ExtractJson(response.OutputText);
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;

            var rating = root.TryGetProperty("systemRating", out var ratingProperty) &&
                         ratingProperty.ValueKind == JsonValueKind.Number
                ? ratingProperty.GetInt32()
                : (int?)null;

            if (rating is < 1 or > 10)
            {
                return new AiAssessmentResult(
                    false,
                    null,
                    null,
                    [],
                    null,
                    "Assessment rating was missing or outside the expected 1-10 range.",
                    response.RawResponse);
            }

            var summary = root.TryGetProperty("summary", out var summaryProperty)
                ? summaryProperty.GetString()
                : null;
            var reasoning = root.TryGetProperty("reasoning", out var reasoningProperty)
                ? reasoningProperty.GetString()
                : null;
            var tags = root.TryGetProperty("tags", out var tagsProperty) && tagsProperty.ValueKind == JsonValueKind.Array
                ? tagsProperty.EnumerateArray()
                    .Where(tag => tag.ValueKind == JsonValueKind.String)
                    .Select(tag => tag.GetString())
                    .Where(tag => !string.IsNullOrWhiteSpace(tag))
                    .Select(tag => tag!)
                    .ToArray()
                : [];

            if (string.IsNullOrWhiteSpace(summary) || string.IsNullOrWhiteSpace(reasoning))
            {
                return new AiAssessmentResult(
                    false,
                    null,
                    null,
                    [],
                    null,
                    "Assessment response did not include the required summary and reasoning fields.",
                    response.RawResponse);
            }

            return new AiAssessmentResult(true, rating, summary, tags, reasoning, null, response.RawResponse);
        }
        catch (Exception exception)
        {
            return new AiAssessmentResult(false, null, null, [], null, exception.Message, response.RawResponse);
        }
    }

    private static string BuildPrompt(string? title, string markdown) =>
        $$"""
        Assess the following article content for usefulness and quality.

        Return strict JSON with this shape:
        {
          "systemRating": 1-10 integer,
          "summary": "short summary",
          "tags": ["tag-one", "tag-two"],
          "reasoning": "brief rationale"
        }

        Title: {{title ?? "(untitled)"}}
        Markdown:
        {{markdown}}
        """;

    private static string Truncate(string content, int maxCharacters) =>
        content.Length <= maxCharacters ? content : content[..maxCharacters];

    private static string ExtractJson(string outputText)
    {
        var trimmed = outputText.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var firstLineBreak = trimmed.IndexOf('\n');
            var lastFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);

            if (firstLineBreak >= 0 && lastFence > firstLineBreak)
            {
                trimmed = trimmed[(firstLineBreak + 1)..lastFence].Trim();
            }
        }

        return trimmed;
    }
}
