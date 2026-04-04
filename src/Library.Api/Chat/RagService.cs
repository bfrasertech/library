using Library.Api.OpenAi;
using Library.Api.Search;

namespace Library.Api.Chat;

public sealed class RagService
{
    private readonly SearchService _searchService;
    private readonly OpenAiClient _openAiClient;

    public RagService(SearchService searchService, OpenAiClient openAiClient)
    {
        _searchService = searchService;
        _openAiClient = openAiClient;
    }

    public async Task<ChatResponse> AskAsync(string question, CancellationToken cancellationToken = default)
    {
        var matches = await _searchService.SearchAsync(question, 5, cancellationToken);
        if (matches.Count == 0)
        {
            return new ChatResponse(
                "I could not find relevant information in your saved articles.",
                []);
        }

        var sources = matches
            .Select(match => new ChatSource(
                match.Record.Id,
                match.Record.Title ?? match.Record.Url,
                match.Record.Url,
                match.Score))
            .ToArray();

        var context = string.Join(
            "\n\n",
            matches.Select((match, index) =>
                $$"""
                [Article {{index + 1}}: "{{match.Record.Title ?? match.Record.Url}}"]
                {{match.Record.AiSummary ?? "(no summary available)"}}
                {{Truncate(match.Record.MarkdownContent ?? string.Empty, 2400)}}
                """));

        var prompt =
            $$"""
            CONTEXT:
            {{context}}

            USER: {{question}}
            """;

        var response = await _openAiClient.CreateChatResponseAsync(
            prompt,
            "You are a helpful research assistant. Answer the user's question based ONLY on the provided context from saved articles. If the context is insufficient, say so. Cite sources by article title.",
            900,
            cancellationToken);

        var answer = string.IsNullOrWhiteSpace(response.OutputText)
            ? "I could not generate an answer from the saved articles."
            : response.OutputText.Trim();

        return new ChatResponse(answer, sources);
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength];
}
