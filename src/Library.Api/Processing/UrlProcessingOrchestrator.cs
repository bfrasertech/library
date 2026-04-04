using Library.Api.Urls;

namespace Library.Api.Processing;

public sealed class UrlProcessingOrchestrator
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UrlProcessingOrchestrator> _logger;

    public UrlProcessingOrchestrator(IServiceScopeFactory scopeFactory, ILogger<UrlProcessingOrchestrator> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public void Enqueue(string urlId)
    {
        _ = Task.Run(() => ProcessBestEffortAsync(urlId));
    }

    private async Task ProcessBestEffortAsync(string urlId)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<UrlRepository>();
            var pipeline = scope.ServiceProvider.GetRequiredService<IUrlProcessingPipeline>();

            var record = await repository.GetByIdAsync(urlId);
            if (record is null)
            {
                _logger.LogWarning("Skipping background processing because URL record {UrlId} no longer exists.", urlId);
                return;
            }

            await repository.UpdateProcessingStateAsync(
                urlId,
                new UrlProcessingStateUpdate("processing", null));

            await pipeline.ProcessAsync(record, CancellationToken.None);

            await repository.UpdateProcessingStateAsync(
                urlId,
                new UrlProcessingStateUpdate("completed", null));
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Best-effort processing failed for URL record {UrlId}.", urlId);

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var repository = scope.ServiceProvider.GetRequiredService<UrlRepository>();

                await repository.UpdateProcessingStateAsync(
                    urlId,
                    new UrlProcessingStateUpdate("failed", exception.Message));
            }
            catch (Exception updateException)
            {
                _logger.LogError(
                    updateException,
                    "Failed to persist the processing failure state for URL record {UrlId}.",
                    urlId);
            }
        }
    }
}
