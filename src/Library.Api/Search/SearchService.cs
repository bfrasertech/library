using Library.Api.Cloudflare;
using Library.Api.OpenAi;
using Library.Api.Urls;
using Microsoft.Extensions.Options;

namespace Library.Api.Search;

public sealed class SearchService
{
    private readonly EmbeddingService _embeddingService;
    private readonly SearchOptions _options;
    private readonly VectorizeClient _vectorizeClient;
    private readonly UrlRepository _urlRepository;

    public SearchService(
        EmbeddingService embeddingService,
        IOptions<SearchOptions> options,
        VectorizeClient vectorizeClient,
        UrlRepository urlRepository)
    {
        _embeddingService = embeddingService;
        _options = options.Value;
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
        var relevantMatches = vectorResult.Matches
            .Where(match => match.Score >= _options.MinimumSimilarityScore)
            .ToArray();

        if (relevantMatches.Length == 0)
        {
            return [];
        }

        var recordMap = (await _urlRepository.GetByIdsAsync(
                relevantMatches.Select(match => match.Id).ToArray(),
                cancellationToken))
            .ToDictionary(record => record.Id, StringComparer.Ordinal);

        return relevantMatches
            .Where(match => recordMap.ContainsKey(match.Id))
            .Select(match => new UrlSearchResultItem(match.Id, match.Score, recordMap[match.Id]))
            .ToArray();
    }
}
