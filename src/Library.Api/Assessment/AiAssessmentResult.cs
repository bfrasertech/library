namespace Library.Api.Assessment;

public sealed record AiAssessmentResult(
    bool Success,
    int? SystemRating,
    string? Summary,
    IReadOnlyList<string> Tags,
    string? Reasoning,
    string? Error,
    string? RawResponse);
