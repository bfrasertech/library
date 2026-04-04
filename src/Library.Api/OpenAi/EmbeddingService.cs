using Microsoft.Extensions.Options;

namespace Library.Api.OpenAi;

public sealed class EmbeddingService
{
    private readonly OpenAiClient _openAiClient;
    private readonly EmbeddingOptions _options;

    public EmbeddingService(OpenAiClient openAiClient, IOptions<EmbeddingOptions> options)
    {
        _openAiClient = openAiClient;
        _options = options.Value;
    }

    public async Task<IReadOnlyList<float>> GenerateAsync(
        string input,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Embedding input must not be empty.", nameof(input));
        }

        var result = await _openAiClient.CreateEmbeddingAsync(input, cancellationToken);

        if (result.Embedding.Count != _options.ExpectedDimensions)
        {
            throw new InvalidOperationException(
                $"Embedding dimension mismatch. Expected {_options.ExpectedDimensions} values but received {result.Embedding.Count}.");
        }

        return result.Embedding;
    }
}
