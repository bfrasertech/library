namespace Library.Api.Urls;

public sealed record SaveUrlCommand(
    string Url,
    string OriginalUrl,
    string? Title,
    string? SourceApplication,
    string? Tags);

public sealed record UrlProcessingStateUpdate(
    string ProcessingStatus,
    string? ProcessingError);

public sealed record UrlAssessmentUpdate(
    int? SystemRating,
    string? AiSummary,
    string? AiTags,
    string? AiReasoning);
