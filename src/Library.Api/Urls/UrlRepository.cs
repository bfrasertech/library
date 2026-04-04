using Library.Api.Database;

namespace Library.Api.Urls;

public sealed class UrlRepository
{
    private readonly D1Client _d1Client;

    public UrlRepository(D1Client d1Client)
    {
        _d1Client = d1Client;
    }

    public async Task<UrlRecord> SaveAsync(SaveUrlCommand command, CancellationToken cancellationToken = default)
    {
        var existingRecord = await GetByUrlAsync(command.Url, cancellationToken);

        if (existingRecord is not null)
        {
            await _d1Client.ExecuteAsync(
                """
                UPDATE urls
                SET original_url = ?,
                    title = ?,
                    source_application = ?,
                    tags = ?,
                    saved_at = strftime('%Y-%m-%dT%H:%M:%fZ', 'now')
                WHERE id = ?
                """,
                [command.OriginalUrl, command.Title, command.SourceApplication, command.Tags, existingRecord.Id],
                cancellationToken);

            return (await GetByIdAsync(existingRecord.Id, cancellationToken))!;
        }

        var id = Guid.NewGuid().ToString("D");

        await _d1Client.ExecuteAsync(
            """
            INSERT INTO urls (
                id,
                url,
                original_url,
                title,
                source_application,
                tags
            )
            VALUES (?, ?, ?, ?, ?, ?)
            """,
            [id, command.Url, command.OriginalUrl, command.Title, command.SourceApplication, command.Tags],
            cancellationToken);

        return (await GetByIdAsync(id, cancellationToken))!;
    }

    public async Task<IReadOnlyList<UrlRecord>> GetAllAsync(
        int pageSize = 50,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        return await _d1Client.QueryAsync<UrlRecord>(
            """
            SELECT
                id,
                url,
                original_url,
                title,
                saved_at,
                processing_status,
                processing_error,
                markdown_content,
                system_rating,
                ai_summary,
                ai_tags,
                ai_reasoning,
                source_application,
                tags
            FROM urls
            ORDER BY saved_at DESC
            LIMIT ? OFFSET ?
            """,
            [pageSize, offset],
            cancellationToken);
    }

    public async Task<UrlRecord?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var records = await _d1Client.QueryAsync<UrlRecord>(
            """
            SELECT
                id,
                url,
                original_url,
                title,
                saved_at,
                processing_status,
                processing_error,
                markdown_content,
                system_rating,
                ai_summary,
                ai_tags,
                ai_reasoning,
                source_application,
                tags
            FROM urls
            WHERE id = ?
            LIMIT 1
            """,
            [id],
            cancellationToken);

        return records.FirstOrDefault();
    }

    public async Task<UrlRecord?> GetByUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var records = await _d1Client.QueryAsync<UrlRecord>(
            """
            SELECT
                id,
                url,
                original_url,
                title,
                saved_at,
                processing_status,
                processing_error,
                markdown_content,
                system_rating,
                ai_summary,
                ai_tags,
                ai_reasoning,
                source_application,
                tags
            FROM urls
            WHERE url = ?
            LIMIT 1
            """,
            [url],
            cancellationToken);

        return records.FirstOrDefault();
    }

    public async Task<bool> DeleteAsync(string id, CancellationToken cancellationToken = default)
    {
        var result = await _d1Client.ExecuteAsync(
            "DELETE FROM urls WHERE id = ?",
            [id],
            cancellationToken);

        return (result.Changes ?? 0) > 0;
    }

    public async Task<bool> UpdateProcessingStateAsync(
        string id,
        UrlProcessingStateUpdate update,
        CancellationToken cancellationToken = default)
    {
        var result = await _d1Client.ExecuteAsync(
            """
            UPDATE urls
            SET processing_status = ?,
                processing_error = ?
            WHERE id = ?
            """,
            [update.ProcessingStatus, update.ProcessingError, id],
            cancellationToken);

        return (result.Changes ?? 0) > 0;
    }

    public async Task<bool> UpdateAssessmentAsync(
        string id,
        UrlAssessmentUpdate update,
        CancellationToken cancellationToken = default)
    {
        var result = await _d1Client.ExecuteAsync(
            """
            UPDATE urls
            SET system_rating = ?,
                ai_summary = ?,
                ai_tags = ?,
                ai_reasoning = ?
            WHERE id = ?
            """,
            [update.SystemRating, update.AiSummary, update.AiTags, update.AiReasoning, id],
            cancellationToken);

        return (result.Changes ?? 0) > 0;
    }
}
