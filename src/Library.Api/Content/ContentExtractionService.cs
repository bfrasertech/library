using ReverseMarkdown;
using SmartReader;

namespace Library.Api.Content;

public sealed class ContentExtractionService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ContentExtractionService> _logger;
    private readonly Converter _markdownConverter = new();

    public ContentExtractionService(HttpClient httpClient, ILogger<ContentExtractionService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ContentExtractionResult> ExtractAsync(string url, CancellationToken cancellationToken = default)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            return new ContentExtractionResult(false, url, null, null, null, "Only absolute http and https URLs are supported.");
        }

        try
        {
            using var response = await _httpClient.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return new ContentExtractionResult(
                    false,
                    url,
                    response.RequestMessage?.RequestUri?.ToString(),
                    null,
                    null,
                    $"The source returned {(int)response.StatusCode} ({response.ReasonPhrase}).");
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (!IsSupportedHtml(mediaType))
            {
                return new ContentExtractionResult(
                    false,
                    url,
                    response.RequestMessage?.RequestUri?.ToString(),
                    null,
                    null,
                    $"Unsupported content type '{mediaType ?? "unknown"}'.");
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(html))
            {
                return new ContentExtractionResult(
                    false,
                    url,
                    response.RequestMessage?.RequestUri?.ToString(),
                    null,
                    null,
                    "The source returned an empty HTML response.");
            }

            var article = Reader.ParseArticle(response.RequestMessage?.RequestUri?.ToString() ?? url, html);
            if (!article.Completed)
            {
                var errorMessage = article.Errors.FirstOrDefault()?.Message ?? "SmartReader could not complete extraction.";
                return new ContentExtractionResult(false, url, article.Uri?.ToString(), article.Title, null, errorMessage);
            }

            if (!article.IsReadable || string.IsNullOrWhiteSpace(article.Content))
            {
                return new ContentExtractionResult(
                    false,
                    url,
                    article.Uri?.ToString(),
                    article.Title,
                    null,
                    "The content could not be extracted into a readable article.");
            }

            var markdown = _markdownConverter.Convert(article.Content).Trim();
            if (string.IsNullOrWhiteSpace(markdown))
            {
                return new ContentExtractionResult(
                    false,
                    url,
                    article.Uri?.ToString(),
                    article.Title,
                    null,
                    "The article HTML was extracted, but markdown conversion produced no content.");
            }

            return new ContentExtractionResult(
                true,
                url,
                article.Uri?.ToString(),
                article.Title,
                markdown,
                null);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(exception, "Timed out while extracting content from {Url}.", url);
            return new ContentExtractionResult(false, url, null, null, null, "The request timed out while fetching the source page.");
        }
        catch (HttpRequestException exception)
        {
            _logger.LogWarning(exception, "HTTP request failed while extracting content from {Url}.", url);
            return new ContentExtractionResult(false, url, null, null, null, $"HTTP fetch failed: {exception.Message}");
        }
    }

    private static bool IsSupportedHtml(string? mediaType) =>
        string.Equals(mediaType, "text/html", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(mediaType, "application/xhtml+xml", StringComparison.OrdinalIgnoreCase);
}
