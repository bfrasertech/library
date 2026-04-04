namespace Library.Api.Assessment;

public sealed class AiAssessmentOptions
{
    public int MaxInputCharacters { get; set; } = 12000;

    public int MaxOutputTokens { get; set; } = 800;
}
