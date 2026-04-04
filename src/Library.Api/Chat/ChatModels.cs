namespace Library.Api.Chat;

public sealed record ChatRequest(string? Question);

public sealed record ChatSource(
    string Id,
    string Title,
    string Url,
    float Score);

public sealed record ChatResponse(
    string Answer,
    IReadOnlyList<ChatSource> Sources);
