using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using TaskTracker.Api.Models;

namespace TaskTracker.Tests;

// Integration tests for PATCH, search, sorting, the overdue filter and /tasks/stats.
public class TaskFeaturesTests : IDisposable
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly TaskTrackerFactory _factory = new();
    private readonly HttpClient _client;

    public TaskFeaturesTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task<TaskItem> CreateAsync(string title, string? description = null, DateTime? dueDate = null)
    {
        var response = await _client.PostAsJsonAsync("/tasks", new { title, description, dueDate });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TaskItem>(Json))!;
    }

    private Task<HttpResponseMessage> PatchAsync(int id, object body) =>
        _client.PatchAsJsonAsync($"/tasks/{id}", body);

    private async Task<List<TaskItem>> ListAsync(string query) =>
        (await _client.GetFromJsonAsync<List<TaskItem>>($"/tasks{query}", Json))!;

    [Fact]
    public async Task Patch_ChangesOnlySentProperties()
    {
        var task = await CreateAsync("Original", "keep me");

        var response = await PatchAsync(task.Id, new { status = "InProgress" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var patched = (await response.Content.ReadFromJsonAsync<TaskItem>(Json))!;
        Assert.Equal("Original", patched.Title);
        Assert.Equal("keep me", patched.Description);
        Assert.Equal(TaskItemStatus.InProgress, patched.Status);
        Assert.NotNull(patched.UpdatedAt);
    }

    [Fact]
    public async Task Patch_EmptyBody_Returns400()
    {
        var task = await CreateAsync("x");

        var response = await PatchAsync(task.Id, new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Patch_BlankTitle_Returns400(string title)
    {
        var task = await CreateAsync("x");

        var response = await PatchAsync(task.Id, new { title });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_TitleTooLong_Returns400()
    {
        var task = await CreateAsync("x");

        var response = await PatchAsync(task.Id, new { title = new string('a', 101) });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Patch_UnknownTask_Returns404()
    {
        var response = await PatchAsync(999, new { title = "x" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompletedAt_IsSetWhenDoneAndClearedWhenReopened()
    {
        var task = await CreateAsync("finish me");
        Assert.Null(task.CompletedAt);

        var done = (await (await PatchAsync(task.Id, new { status = "Done" })).Content.ReadFromJsonAsync<TaskItem>(Json))!;
        Assert.NotNull(done.CompletedAt);

        var reopened = (await (await PatchAsync(task.Id, new { status = "Todo" })).Content.ReadFromJsonAsync<TaskItem>(Json))!;
        Assert.Null(reopened.CompletedAt);
    }

    [Fact]
    public async Task Put_SetsUpdatedAtAndCompletedAt()
    {
        var task = await CreateAsync("before");

        var response = await _client.PutAsJsonAsync($"/tasks/{task.Id}", new { title = "after", status = "Done" });
        var updated = (await response.Content.ReadFromJsonAsync<TaskItem>(Json))!;

        Assert.NotNull(updated.UpdatedAt);
        Assert.NotNull(updated.CompletedAt);
    }

    [Fact]
    public async Task Search_MatchesTitleAndDescriptionIgnoringCase()
    {
        await CreateAsync("Write Docs");
        await CreateAsync("Other", "update the DOCS site");
        await CreateAsync("Unrelated");

        var found = await ListAsync("?search=docs");

        Assert.Equal(2, found.Count);
    }

    [Fact]
    public async Task Search_TreatsPercentAndUnderscoreLiterally()
    {
        await CreateAsync("100% done");
        await CreateAsync("plain");

        Assert.Single(await ListAsync("?search=%25"));
        Assert.Empty(await ListAsync("?search=_"));
    }

    [Fact]
    public async Task Sort_ByTitleDescending()
    {
        await CreateAsync("b");
        await CreateAsync("a");
        await CreateAsync("c");

        var titles = (await ListAsync("?sort=title&order=desc")).Select(t => t.Title);

        Assert.Equal(["c", "b", "a"], titles);
    }

    [Fact]
    public async Task Sort_ByDueDate_PutsTasksWithoutDueDateLast()
    {
        await CreateAsync("none");
        await CreateAsync("late", dueDate: new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await CreateAsync("early", dueDate: new DateTime(2029, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        var titles = (await ListAsync("?sort=dueDate")).Select(t => t.Title);

        Assert.Equal(["early", "late", "none"], titles);
    }

    [Theory]
    [InlineData("?sort=nope")]
    [InlineData("?order=sideways")]
    public async Task Sort_InvalidValue_Returns400(string query)
    {
        var response = await _client.GetAsync($"/tasks{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Overdue_ReturnsOnlyPastDueTasksThatAreNotDone()
    {
        var past = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        await CreateAsync("overdue", dueDate: past);
        var finished = await CreateAsync("done late", dueDate: past);
        await PatchAsync(finished.Id, new { status = "Done" });
        await CreateAsync("future", dueDate: new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        await CreateAsync("no date");

        var overdue = await ListAsync("?overdue=true");

        Assert.Equal("overdue", Assert.Single(overdue).Title);
    }

    [Fact]
    public async Task Stats_CountsByStatusAndOverdue()
    {
        await CreateAsync("todo", dueDate: new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        var started = await CreateAsync("started");
        var finished = await CreateAsync("finished");
        await PatchAsync(started.Id, new { status = "InProgress" });
        await PatchAsync(finished.Id, new { status = "Done" });

        var stats = (await _client.GetFromJsonAsync<TaskStats>("/tasks/stats", Json))!;

        Assert.Equal(new TaskStats(Total: 3, Todo: 1, InProgress: 1, Done: 1, Overdue: 1), stats);
    }

    [Fact]
    public async Task Stats_EmptyDatabase_ReturnsZeros()
    {
        var stats = (await _client.GetFromJsonAsync<TaskStats>("/tasks/stats", Json))!;

        Assert.Equal(new TaskStats(0, 0, 0, 0, 0), stats);
    }
}
