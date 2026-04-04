using Library.Api.Urls;

namespace Library.Api.Processing;

public sealed class NoOpUrlProcessingPipeline : IUrlProcessingPipeline
{
    private readonly ILogger<NoOpUrlProcessingPipeline> _logger;

    public NoOpUrlProcessingPipeline(ILogger<NoOpUrlProcessingPipeline> logger)
    {
        _logger = logger;
    }

    public Task ProcessAsync(UrlRecord record, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Best-effort processing shell executed for URL {UrlId}. No extraction or AI pipeline is wired yet.",
            record.Id);

        return Task.CompletedTask;
    }
}
