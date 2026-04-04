using System.Text.Json.Serialization;

namespace Library.Api.Urls;

public sealed record UrlRecord
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("url")]
    public required string Url { get; init; }

    [JsonPropertyName("original_url")]
    public required string OriginalUrl { get; init; }

    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("saved_at")]
    public required string SavedAt { get; init; }

    [JsonPropertyName("processing_status")]
    public required string ProcessingStatus { get; init; }

    [JsonPropertyName("processing_error")]
    public string? ProcessingError { get; init; }

    [JsonPropertyName("markdown_content")]
    public string? MarkdownContent { get; init; }

    [JsonPropertyName("system_rating")]
    public int? SystemRating { get; init; }

    [JsonPropertyName("ai_summary")]
    public string? AiSummary { get; init; }

    [JsonPropertyName("ai_tags")]
    public string? AiTags { get; init; }

    [JsonPropertyName("ai_reasoning")]
    public string? AiReasoning { get; init; }

    [JsonPropertyName("source_application")]
    public string? SourceApplication { get; init; }

    [JsonPropertyName("tags")]
    public string? Tags { get; init; }
}
