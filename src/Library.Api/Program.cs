using System.Text.Json;
using Library.Api.Cloudflare;
using Library.Api.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables();

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = false;
});

builder.Services
    .AddOptions<CloudflareOptions>()
    .Bind(builder.Configuration.GetSection("Cloudflare"))
    .PostConfigure(options => options.ApplyOverrides(builder.Configuration));

builder.Services.AddHttpClient<D1Client>(client =>
{
    client.BaseAddress = new Uri("https://api.cloudflare.com/client/v4/");
});

var app = builder.Build();

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

app.Run();
