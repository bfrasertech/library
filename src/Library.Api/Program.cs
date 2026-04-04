using System.Text.Json;
using Library.Api.Assessment;
using Library.Api.Chat;
using Library.Api.Cloudflare;
using Library.Api.Content;
using Library.Api.Database;
using Library.Api.OpenAi;
using Library.Api.Processing;
using Library.Api.Search;
using Library.Api.Urls;

var builder = WebApplication.CreateBuilder(args);
const string LocalFrontendCorsPolicy = "LocalFrontend";

builder.Configuration.AddEnvironmentVariables();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = false;
});

builder.Services.AddCors(options =>
{
    options.AddPolicy(LocalFrontendCorsPolicy, policy =>
    {
        policy
            .WithOrigins("http://localhost:3000", "http://127.0.0.1:3000")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddOptions<CloudflareOptions>()
    .Bind(builder.Configuration.GetSection("Cloudflare"))
    .PostConfigure(options => options.ApplyOverrides(builder.Configuration));
builder.Services
    .AddOptions<OpenAiOptions>()
    .Bind(builder.Configuration.GetSection("OpenAI"))
    .PostConfigure(options => options.ApplyOverrides(builder.Configuration));
builder.Services
    .AddOptions<AiAssessmentOptions>()
    .Bind(builder.Configuration.GetSection("Assessment"));
builder.Services
    .AddOptions<EmbeddingOptions>()
    .Bind(builder.Configuration.GetSection("Embeddings"));

builder.Services.AddHttpClient<D1Client>(client =>
{
    client.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
});
builder.Services.AddHttpClient<VectorizeClient>(client =>
{
    client.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient<ContentExtractionService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Library.Api/1.0 (+https://example.local/library)");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AllowAutoRedirect = true,
    MaxAutomaticRedirections = 10
});
builder.Services.AddHttpClient<OpenAiClient>(client =>
{
    client.BaseAddress = new Uri("https://api.openai.com/v1/");
    client.Timeout = TimeSpan.FromSeconds(60);
});
builder.Services.AddScoped<AiAssessmentService>();
builder.Services.AddScoped<EmbeddingService>();
builder.Services.AddScoped<RagService>();
builder.Services.AddScoped<SearchService>();
builder.Services.AddScoped<UrlDeletionService>();
builder.Services.AddScoped<UrlRepository>();
builder.Services.AddSingleton<UrlProcessingOrchestrator>();
builder.Services.AddScoped<IUrlProcessingPipeline, LibraryPipeline>();

var app = builder.Build();

app.UseCors(LocalFrontendCorsPolicy);

app.MapGet("/", (IConfiguration configuration) =>
{
    var environment = app.Environment.EnvironmentName;
    var contentBaseUrl = configuration["ContentSource:BaseUrl"] ?? "not-configured";

    return Results.Ok(new
    {
        service = "Library.Api",
        status = "ok",
        environment,
        contentSource = new
        {
            baseUrl = contentBaseUrl
        }
    });
});

app.MapGet("/health", () => Results.Ok(new
{
    status = "healthy"
}));

var urls = app.MapGroup("/api/urls");

urls.MapPost("/", async (
    SaveUrlRequest request,
    UrlRepository repository,
    UrlProcessingOrchestrator orchestrator,
    CancellationToken cancellationToken) =>
{
    if (!TryBuildSaveUrlCommand(request, out var command, out var error))
    {
        return Results.BadRequest(new { error });
    }

    var record = await repository.SaveAsync(command!, cancellationToken);
    orchestrator.Enqueue(record.Id);
    return Results.Accepted($"/api/urls/{record.Id}", record);
});

urls.MapGet("/", async ([AsParameters] UrlListRequest request, UrlRepository repository, CancellationToken cancellationToken) =>
{
    if (request.PageSize <= 0 || request.PageSize > 200)
    {
        return Results.BadRequest(new { error = "pageSize must be between 1 and 200." });
    }

    if (request.Offset < 0)
    {
        return Results.BadRequest(new { error = "offset must be zero or greater." });
    }

    var records = await repository.GetAllAsync(request.PageSize, request.Offset, cancellationToken);
    return Results.Ok(records);
});

urls.MapGet("/search", async ([AsParameters] UrlSearchRequest request, SearchService searchService, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Q))
    {
        return Results.BadRequest(new { error = "q is required." });
    }

    if (request.TopK <= 0 || request.TopK > 20)
    {
        return Results.BadRequest(new { error = "topK must be between 1 and 20." });
    }

    var results = await searchService.SearchAsync(request.Q, request.TopK, cancellationToken);
    return Results.Ok(results);
});

urls.MapGet("/{id}", async (string id, UrlRepository repository, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(id))
    {
        return Results.BadRequest(new { error = "id is required." });
    }

    var record = await repository.GetByIdAsync(id, cancellationToken);
    return record is null ? Results.NotFound() : Results.Ok(record);
});

urls.MapDelete("/{id}", async (string id, UrlDeletionService deletionService, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(id))
    {
        return Results.BadRequest(new { error = "id is required." });
    }

    var result = await deletionService.DeleteAsync(id, cancellationToken);
    if (result.Success)
    {
        return Results.NoContent();
    }

    if (result.CleanupBlocked)
    {
        return Results.Problem(
            title: "Delete blocked by vector cleanup failure.",
            detail: result.ErrorMessage,
            statusCode: StatusCodes.Status409Conflict);
    }

    return result.ErrorCode == "not_found"
        ? Results.NotFound()
        : Results.BadRequest(new { error = result.ErrorMessage });
});

app.MapPost("/api/chat", async (ChatRequest request, RagService ragService, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Question))
    {
        return Results.BadRequest(new { error = "question is required." });
    }

    var response = await ragService.AskAsync(request.Question, cancellationToken);
    return Results.Ok(response);
});

app.Run();

static bool TryBuildSaveUrlCommand(
    SaveUrlRequest request,
    out SaveUrlCommand? command,
    out string? error)
{
    command = null;
    error = null;

    if (string.IsNullOrWhiteSpace(request.Url))
    {
        error = "url is required.";
        return false;
    }

    if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var parsedUrl) ||
        (parsedUrl.Scheme != Uri.UriSchemeHttp && parsedUrl.Scheme != Uri.UriSchemeHttps))
    {
        error = "url must be a valid absolute http or https URL.";
        return false;
    }

    var normalizedUrl = parsedUrl.ToString();
    var originalUrl = string.IsNullOrWhiteSpace(request.OriginalUrl) ? normalizedUrl : request.OriginalUrl.Trim();

    command = new SaveUrlCommand(
        normalizedUrl,
        originalUrl,
        request.Title?.Trim(),
        request.SourceApplication?.Trim(),
        request.Tags?.Trim());

    return true;
}
