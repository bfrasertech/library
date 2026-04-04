namespace Library.Api.OpenAi;

public sealed record OpenAiResponseRequest(
    string Model,
    string Input,
    string? Instructions = null,
    int? MaxOutputTokens = null);

public sealed record OpenAiResponseResult(
    string Id,
    string Model,
    string Status,
    string OutputText,
    string RawResponse);

public sealed record OpenAiEmbeddingResult(
    string Model,
    IReadOnlyList<float> Embedding,
    string RawResponse);
