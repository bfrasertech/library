using System.Text.Json;
using Library.Api.Assessment;
using Library.Api.Cloudflare;
using Library.Api.Content;
using Library.Api.OpenAi;
using Library.Api.Urls;

namespace Library.Api.Processing;

public sealed class LibraryPipeline : IUrlProcessingPipeline
{
    private readonly ContentExtractionService _contentExtractionService;
    private readonly AiAssessmentService _aiAssessmentService;
    private readonly EmbeddingService _embeddingService;
    private readonly VectorizeClient _vectorizeClient;
    private readonly UrlRepository _repository;

    public LibraryPipeline(
        ContentExtractionService contentExtractionService,
        AiAssessmentService aiAssessmentService,
        EmbeddingService embeddingService,
        VectorizeClient vectorizeClient,
        UrlRepository repository)
    {
        _contentExtractionService = contentExtractionService;
        _aiAssessmentService = aiAssessmentService;
        _embeddingService = embeddingService;
        _vectorizeClient = vectorizeClient;
        _repository = repository;
    }

    public async Task ProcessAsync(UrlRecord record, CancellationToken cancellationToken)
    {
        var extraction = await _contentExtractionService.ExtractAsync(record.Url, cancellationToken);
        if (!extraction.Success || string.IsNullOrWhiteSpace(extraction.Markdown))
        {
            throw new InvalidOperationException(extraction.Error ?? "Content extraction failed.");
        }

        await _repository.UpdateExtractedContentAsync(
            record.Id,
            extraction.Title,
            extraction.Markdown,
            cancellationToken);

        var refreshedRecord = await _repository.GetByIdAsync(record.Id, cancellationToken)
            ?? throw new InvalidOperationException($"URL record {record.Id} was not found after extraction.");

        var assessment = await _aiAssessmentService.AssessAsync(
            extraction.Markdown,
            refreshedRecord.Title,
            cancellationToken);

        if (!assessment.Success)
        {
            throw new InvalidOperationException(assessment.Error ?? "AI assessment failed.");
        }

        await _repository.UpdateAssessmentAsync(
            record.Id,
            new UrlAssessmentUpdate(
                assessment.SystemRating,
                assessment.Summary,
                JsonSerializer.Serialize(assessment.Tags),
                assessment.Reasoning),
            cancellationToken);

        var vector = await _embeddingService.GenerateAsync(extraction.Markdown, cancellationToken);
        var mutation = await _vectorizeClient.UpsertAsync(
            [
                new VectorizeVector(
                    record.Id,
                    vector,
                    new Dictionary<string, object?>
                    {
                        ["urlId"] = record.Id,
                        ["url"] = record.Url
                    })
            ],
            cancellationToken);

        if (!mutation.Success)
        {
            throw new InvalidOperationException("Vectorize upsert did not report success.");
        }
    }
}
