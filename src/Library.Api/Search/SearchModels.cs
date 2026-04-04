using Library.Api.Urls;

namespace Library.Api.Search;

public sealed record UrlSearchRequest(string? Q, int TopK = 5);

public sealed record UrlSearchResultItem(
    string Id,
    float Score,
    UrlRecord Record);
