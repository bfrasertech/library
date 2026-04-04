namespace Library.Api.Urls;

public sealed record SaveUrlRequest(
    string? Url,
    string? OriginalUrl,
    string? Title,
    string? SourceApplication,
    string? Tags);

public sealed record UrlListRequest(int PageSize = 50, int Offset = 0);
