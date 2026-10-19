using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskTracker.Api.Models;

namespace TaskTracker.Tests;

// Integration tests: real HTTP requests against the real API and a real (temporary) SQLite database.
// xUnit creates a new instance of this class for every test, so each test gets a fresh API and database.
public class TasksApiTests : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly TaskTrackerFactory _factory = new();
    private readonly HttpClient _client;

    public TasksApiTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<TaskItem> CreateAsync(string title, string? description = null)
    {
        var response = await _client.PostAsJsonAsync("/tasks", new { title, description });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TaskItem>(Json))!;
    }

    private static async Task<JsonElement> ReadErrorsAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("errors").Clone();
    }

    [Fact]
    public async Task Create_ValidTask_Returns201WithLocationAndBody()
    {
        var response = await _client.PostAsJsonAsync("/tasks", new { title = "Write tests", description = "stage 5" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskItem>(Json);
        Assert.NotNull(task);
        Assert.True(task.Id > 0);
        Assert.Equal("Write tests", task.Title);
        Assert.Equal("stage 5", task.Description);
        Assert.Equal(TaskItemStatus.Todo, task.Status);
        Assert.Equal($"/tasks/{task.Id}", response.Headers.Location?.OriginalString);
    }

    [Fact]
    public async Task Create_TrimsTitleAndDescription()
    {
        var task = await CreateAsync("  padded  ", "  text  ");

        Assert.Equal("padded", task.Title);
        Assert.Equal("text", task.Description);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_BlankTitle_Returns400WithTitleError(string title)
    {
        var response = await _client.PostAsJsonAsync("/tasks", new { title });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await ReadErrorsAsync(response);
        Assert.True(errors.TryGetProperty("title", out _), "Expected a validation error for 'title'.");
    }

    [Fact]
    public async Task Create_MissingTitle_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/tasks", new { description = "no title" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_TitleLongerThan100_Returns400()
    {
        var response = await _client.PostAsJsonAsync("/tasks", new { title = new string('x', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Create_MalformedJson_Returns400()
    {
        var content = new StringContent("{bad json", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PostAsync("/tasks", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingTask_Returns200()
    {
        var created = await CreateAsync("Find me");

        var response = await _client.GetAsync($"/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var task = await response.Content.ReadFromJsonAsync<TaskItem>(Json);
        Assert.Equal("Find me", task!.Title);
    }

    [Fact]
    public async Task GetById_MissingTask_Returns404()
    {
        var response = await _client.GetAsync("/tasks/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetAll_ReturnsTasksOrderedById()
    {
        await CreateAsync("one");
        await CreateAsync("two");

        var tasks = await _client.GetFromJsonAsync<List<TaskItem>>("/tasks", Json);

        Assert.Equal(["one", "two"], tasks!.Select(t => t.Title));
    }

    [Fact]
    public async Task GetAll_FilterByStatus_ReturnsOnlyMatchingTasks()
    {
        var first = await CreateAsync("will be done");
        await CreateAsync("stays todo");
        await _client.PutAsJsonAsync($"/tasks/{first.Id}", new { title = "will be done", status = "Done" });

        var done = await _client.GetFromJsonAsync<List<TaskItem>>("/tasks?status=Done", Json);
        var todo = await _client.GetFromJsonAsync<List<TaskItem>>("/tasks?status=Todo", Json);

        Assert.Equal(["will be done"], done!.Select(t => t.Title));
        Assert.Equal(["stays todo"], todo!.Select(t => t.Title));
    }

    [Fact]
    public async Task GetAll_InvalidStatusFilter_Returns400()
    {
        var response = await _client.GetAsync("/tasks?status=Bogus");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Update_ExistingTask_Returns200AndPersistsChanges()
    {
        var created = await CreateAsync("old title", "old description");

        var response = await _client.PutAsJsonAsync(
            $"/tasks/{created.Id}",
            new { title = "new title", status = "InProgress", dueDate = "2026-12-31T00:00:00Z" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var reloaded = await _client.GetFromJsonAsync<TaskItem>($"/tasks/{created.Id}", Json);
        Assert.Equal("new title", reloaded!.Title);
        Assert.Null(reloaded.Description); // PUT replaces the whole task, so omitted fields are cleared
        Assert.Equal(TaskItemStatus.InProgress, reloaded.Status);
        Assert.Equal(new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc), reloaded.DueDate);
        Assert.Equal(created.CreatedAt, reloaded.CreatedAt);
    }

    [Fact]
    public async Task Update_MissingTask_Returns404()
    {
        var response = await _client.PutAsJsonAsync("/tasks/999", new { title = "x", status = "Done" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_MissingStatus_Returns400WithStatusError()
    {
        var created = await CreateAsync("task");

        var response = await _client.PutAsJsonAsync($"/tasks/{created.Id}", new { title = "task" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = await ReadErrorsAsync(response);
        Assert.True(errors.TryGetProperty("status", out _), "Expected a validation error for 'status'.");
    }

    [Theory]
    [InlineData("\"Nope\"")]
    [InlineData("1")]
    public async Task Update_InvalidStatusValue_Returns400(string statusJson)
    {
        var created = await CreateAsync("task");
        var content = new StringContent(
            $$"""{"title":"task","status":{{statusJson}}}""", System.Text.Encoding.UTF8, "application/json");

        var response = await _client.PutAsync($"/tasks/{created.Id}", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Delete_ExistingTask_Returns204AndTaskIsGone()
    {
        var created = await CreateAsync("delete me");

        var deleteResponse = await _client.DeleteAsync($"/tasks/{created.Id}");
        var getResponse = await _client.GetAsync($"/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_MissingTask_Returns404()
    {
        var response = await _client.DeleteAsync("/tasks/999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Timestamps_AreReturnedAsUtcWithZSuffix()
    {
        var created = await CreateAsync("time check");

        // Read the raw JSON: a deserializer could hide a missing "Z".
        var raw = await _client.GetStringAsync($"/tasks/{created.Id}");
        using var doc = JsonDocument.Parse(raw);

        Assert.EndsWith("Z", doc.RootElement.GetProperty("createdAt").GetString());
    }

    [Fact]
    public async Task OpenApiDocument_IsAvailableInDevelopment()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
