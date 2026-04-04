namespace Library.Api.Cloudflare;

public sealed record VectorizeVector(
    string Id,
    IReadOnlyList<float> Values,
    IReadOnlyDictionary<string, object?>? Metadata = null);

public sealed record VectorizeMutationResult(
    string? MutationId,
    bool Success,
    string RawResponse);

public sealed record VectorizeQueryMatch(
    string Id,
    float Score,
    IReadOnlyDictionary<string, object?>? Metadata);

public sealed record VectorizeQueryResult(
    IReadOnlyList<VectorizeQueryMatch> Matches,
    int Count,
    string RawResponse);
