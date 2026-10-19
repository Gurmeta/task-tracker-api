using System.Text.Json.Serialization;
using TaskTracker.Api.Endpoints;
using TaskTracker.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Enum values are written as words ("Todo") instead of numbers (0) in JSON,
// and numbers are rejected as input.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter(allowIntegerValues: false)));

// Singleton: one shared instance, because the in-memory list must survive between requests.
builder.Services.AddSingleton<ITaskStore, InMemoryTaskStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapTaskEndpoints();

app.Run();

// Makes Program visible to the test project (WebApplicationFactory<Program>).
public partial class Program;
