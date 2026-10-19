using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using TaskTracker.Api.Data;
using TaskTracker.Api.Endpoints;
using TaskTracker.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Enum values are written as words ("Todo") instead of numbers (0) in JSON,
// and numbers are rejected as input.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

// The connection string comes from configuration (appsettings.json or the
// ConnectionStrings__Default environment variable), so nothing is hardcoded here.
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddHealthChecks();

// Scoped: one DbContext and one store per HTTP request.
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));
builder.Services.AddScoped<ITaskStore, EfTaskStore>();

var app = builder.Build();

// Create or update the database schema at startup by applying pending migrations.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Liveness probe for Docker, Kubernetes and load balancers: 200 means the process is up.
app.MapHealthChecks("/health");

app.MapTaskEndpoints();

app.Run();

// Makes Program visible to the test project (WebApplicationFactory<Program>).
public partial class Program;
