using Library.Api.Cloudflare;
using Library.Api.OpenAi;
using Library.Api.Urls;

namespace Library.Api.Search;

public sealed class SearchService
{
    private readonly EmbeddingService _embeddingService;
    private readonly VectorizeClient _vectorizeClient;
    private readonly UrlRepository _urlRepository;

    public SearchService(
        EmbeddingService embeddingService,
        VectorizeClient vectorizeClient,
        UrlRepository urlRepository)
    {
        _embeddingService = embeddingService;
        _vectorizeClient = vectorizeClient;
        _urlRepository = urlRepository;
    }

    public async Task<IReadOnlyList<UrlSearchResultItem>> SearchAsync(
        string query,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        var embedding = await _embeddingService.GenerateAsync(query, cancellationToken);
        var vectorResult = await _vectorizeClient.QueryAsync(embedding, topK, cancellationToken);

        if (vectorResult.Matches.Count == 0)
        {
            return [];
        }

        var recordMap = (await _urlRepository.GetByIdsAsync(
                vectorResult.Matches.Select(match => match.Id).ToArray(),
                cancellationToken))
            .ToDictionary(record => record.Id, StringComparer.Ordinal);

        return vectorResult.Matches
            .Where(match => recordMap.ContainsKey(match.Id))
            .Select(match => new UrlSearchResultItem(match.Id, match.Score, recordMap[match.Id]))
            .ToArray();
    }
}
