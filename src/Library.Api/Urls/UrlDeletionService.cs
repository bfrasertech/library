using Library.Api.Cloudflare;

namespace Library.Api.Urls;

public sealed class UrlDeletionService
{
    private readonly UrlRepository _repository;
    private readonly VectorizeClient _vectorizeClient;

    public UrlDeletionService(UrlRepository repository, VectorizeClient vectorizeClient)
    {
        _repository = repository;
        _vectorizeClient = vectorizeClient;
    }

    public async Task<UrlDeleteResult> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var record = await _repository.GetByIdAsync(id, cancellationToken);
        if (record is null)
        {
            return new UrlDeleteResult(false, false, "not_found", "The URL record was not found.");
        }

        if (string.Equals(record.ProcessingStatus, "completed", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var vectorDelete = await _vectorizeClient.DeleteAsync([id], cancellationToken);
                if (!vectorDelete.Success)
                {
                    return new UrlDeleteResult(
                        false,
                        true,
                        "vector_delete_failed",
                        "Vector cleanup was required but did not report success.");
                }
            }
            catch (Exception exception)
            {
                return new UrlDeleteResult(
                    false,
                    true,
                    "vector_delete_failed",
                    $"Vector cleanup failed: {exception.Message}");
            }
        }

        var deleted = await _repository.DeleteAsync(id, cancellationToken);
        return deleted
            ? new UrlDeleteResult(true, false, null, null)
            : new UrlDeleteResult(false, false, "not_found", "The URL record was not found.");
    }
}

public sealed record UrlDeleteResult(
    bool Success,
    bool CleanupBlocked,
    string? ErrorCode,
    string? ErrorMessage);
