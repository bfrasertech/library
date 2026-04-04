namespace Library.Api.Database;

public sealed record D1ExecutionResult(
    double? Changes,
    double? RowsRead,
    double? RowsWritten,
    double? DurationMs,
    bool Success);

public sealed record D1ResponseEnvelope<T>(
    bool Success,
    IReadOnlyList<D1Result<T>> Result,
    IReadOnlyList<D1Error> Errors);

public sealed record D1Result<T>(
    IReadOnlyList<T> Results,
    D1Meta? Meta,
    bool Success,
    string? Error);

public sealed record D1Meta(
    double? ChangedDb,
    double? Changes,
    double? Duration,
    double? LastRowId,
    double? RowsRead,
    double? RowsWritten,
    string? ServedByColo,
    bool? ServedByPrimary,
    string? ServedByRegion);

public sealed record D1Error(int Code, string Message);

public sealed record D1QueryEnvelope<T>(
    IReadOnlyList<T> Results,
    D1Meta? Meta,
    bool Success);
