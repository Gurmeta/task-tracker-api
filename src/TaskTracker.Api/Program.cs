using System.Text.Json.Serialization;
using TaskTracker.Api.Models;
using TaskTracker.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

// Enum values are written as words ("Todo") instead of numbers (0) in JSON.
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Singleton: one shared instance, because the in-memory list must survive between requests.
builder.Services.AddSingleton<ITaskStore, InMemoryTaskStore>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/tasks", (ITaskStore store) => Results.Ok(store.GetAll()));

app.MapGet("/tasks/{id:int}", (int id, ITaskStore store) =>
{
    var task = store.GetById(id);
    return task is null ? Results.NotFound() : Results.Ok(task);
});

app.MapPost("/tasks", (TaskItem task, ITaskStore store) =>
{
    var created = store.Add(task);
    return Results.Created($"/tasks/{created.Id}", created);
});

app.Run();

// Makes Program visible to the test project (WebApplicationFactory<Program>).
public partial class Program;
