using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddEnvironmentVariables(prefix: "LIBRARYLITE_");

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = false;
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
