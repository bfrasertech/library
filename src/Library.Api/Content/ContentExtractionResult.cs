namespace Library.Api.Content;

public sealed record ContentExtractionResult(
    bool Success,
    string SourceUrl,
    string? FinalUrl,
    string? Title,
    string? Markdown,
    string? Error);
