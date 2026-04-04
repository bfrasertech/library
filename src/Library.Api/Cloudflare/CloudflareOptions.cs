namespace Library.Api.Cloudflare;

public sealed class CloudflareOptions
{
    public string? AccountId { get; set; }

    public string? ApiToken { get; set; }

    public string? D1DatabaseId { get; set; }

    public string? D1DatabaseName { get; set; }

    public string? VectorizeIndexName { get; set; }

    public void ApplyOverrides(IConfiguration configuration)
    {
        AccountId = configuration["CLOUDFLARE_ACCOUNT_ID"] ?? AccountId;
        ApiToken = configuration["CLOUDFLARE_API_TOKEN"] ?? ApiToken;
        D1DatabaseId = configuration["CLOUDFLARE_D1_DATABASE_ID"] ?? D1DatabaseId;
        D1DatabaseName = configuration["CLOUDFLARE_D1_DATABASE_NAME"] ?? D1DatabaseName;
        VectorizeIndexName = configuration["CLOUDFLARE_VECTORIZE_INDEX_NAME"] ?? VectorizeIndexName;
    }

    public void ValidateD1Configuration()
    {
        var missingValues = new List<string>();

        if (string.IsNullOrWhiteSpace(AccountId))
        {
            missingValues.Add(nameof(AccountId));
        }

        if (string.IsNullOrWhiteSpace(ApiToken))
        {
            missingValues.Add(nameof(ApiToken));
        }

        if (string.IsNullOrWhiteSpace(D1DatabaseId))
        {
            missingValues.Add(nameof(D1DatabaseId));
        }

        if (missingValues.Count == 0)
        {
            return;
        }

        throw new InvalidOperationException(
            $"Cloudflare D1 configuration is incomplete. Missing values: {string.Join(", ", missingValues)}.");
    }
}
