using Library.Api.Urls;

namespace Library.Api.Processing;

public interface IUrlProcessingPipeline
{
    Task ProcessAsync(UrlRecord record, CancellationToken cancellationToken);
}
