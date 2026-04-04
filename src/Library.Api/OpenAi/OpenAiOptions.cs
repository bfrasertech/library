namespace Library.Api.OpenAi;

public sealed class OpenAiOptions
{
    public string? ApiKey { get; set; }

    public string EmbeddingModel { get; set; } = "text-embedding-3-small";

    public string AssessmentModel { get; set; } = "gpt-4.1-mini";

    public string ChatModel { get; set; } = "gpt-4.1-mini";

    public void ApplyOverrides(IConfiguration configuration)
    {
        ApiKey = configuration["OPENAI_API_KEY"] ?? ApiKey;
        EmbeddingModel = configuration["OPENAI_EMBEDDING_MODEL"] ?? EmbeddingModel;
        AssessmentModel = configuration["OPENAI_ASSESSMENT_MODEL"] ?? AssessmentModel;
        ChatModel = configuration["OPENAI_CHAT_MODEL"] ?? ChatModel;
    }

    public void ValidateApiKey()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey))
        {
            return;
        }

        throw new InvalidOperationException("OpenAI API configuration is incomplete. Missing ApiKey.");
    }
}
