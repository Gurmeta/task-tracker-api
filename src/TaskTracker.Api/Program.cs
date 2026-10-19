using System.Text.Json.Serialization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Api;
using TaskTracker.Api.Data;
using TaskTracker.Api.Endpoints;
using TaskTracker.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// The OpenAPI document reports the same version as GET /version.
// ASP.NET adds the address the app was started on (for example http://localhost:5000),
// which is wrong once the document is published. With no "servers" entry, OpenAPI
// clients use the address the document was loaded from.
builder.Services.AddOpenApi(options =>
    options.AddDocumentTransformer((document, _, _) =>
    {
        document.Info.Version = AppVersion.Current;
        document.Servers?.Clear();
        return Task.CompletedTask;
    }));

// Enum values are written as words ("Todo") instead of numbers (0) in JSON,
// and numbers are rejected as input.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

// The connection string comes from configuration (appsettings.json or the
// ConnectionStrings__Default environment variable), so nothing is hardcoded here.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddHealthChecks();

// Error responses (404, malformed JSON, unknown status filter, unexpected failures) get a
// problem details JSON body instead of an empty one.
builder.Services.AddProblemDetails();

// Scoped: one DbContext and one store per HTTP request.
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<ITaskStore, EfTaskStore>();

var app = builder.Build();

// Create or update the database schema at startup by applying pending migrations.
try
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}
catch (SqliteException ex)
{
    // The most common cause is a folder that does not exist or is not writable by the
    // container user (for example a bind-mounted /data). Say so instead of printing a stack trace.
    var path = new SqliteConnectionStringBuilder(connectionString).DataSource;
    app.Logger.LogCritical(
        "Cannot open or update the database '{Path}' ({Message}). Check that its folder exists and is writable by the user running the app.",
        path, ex.Message);
    return 1;
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    // Unhandled exceptions become a generic 500 problem details body; no stack trace leaks.
    app.UseExceptionHandler();
}

app.UseStatusCodePages();

// Liveness probe for Docker, Kubernetes and load balancers: 200 means the process is up.
app.MapHealthChecks("/health");

app.MapVersionEndpoint();
app.MapTaskEndpoints();

app.Run();
return 0;

// Makes Program visible to the test project (WebApplicationFactory<Program>).
public partial class Program;
