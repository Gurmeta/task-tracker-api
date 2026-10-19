using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace TaskTracker.Tests;

// Every error response should carry a problem details JSON body (RFC 9457),
// so a client can tell what went wrong without guessing from the status code alone.
public class ErrorResponseTests : IDisposable
{
    private readonly TaskTrackerFactory _factory = new();
    private readonly HttpClient _client;

    public ErrorResponseTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static async Task<JsonElement> ReadProblemAsync(HttpResponseMessage response, HttpStatusCode expected)
    {
        Assert.Equal(expected, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var problem = doc.RootElement.Clone();
        Assert.Equal((int)expected, problem.GetProperty("status").GetInt32());
        return problem;
    }

    [Fact]
    public async Task GetMissingTask_Returns404ProblemThatNamesTheId()
    {
        var problem = await ReadProblemAsync(await _client.GetAsync("/tasks/999"), HttpStatusCode.NotFound);

        Assert.Equal("Task not found", problem.GetProperty("title").GetString());
        Assert.Contains("999", problem.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task UpdateMissingTask_Returns404Problem()
    {
        var response = await _client.PutAsJsonAsync("/tasks/999", new { title = "x", status = "Done" });

        await ReadProblemAsync(response, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteMissingTask_Returns404Problem()
    {
        await ReadProblemAsync(await _client.DeleteAsync("/tasks/999"), HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task UnknownStatusFilter_Returns400Problem()
    {
        await ReadProblemAsync(await _client.GetAsync("/tasks?status=Bogus"), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MalformedJson_Returns400Problem()
    {
        var content = new StringContent("{bad json", System.Text.Encoding.UTF8, "application/json");

        await ReadProblemAsync(await _client.PostAsync("/tasks", content), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InvalidDueDate_Returns400Problem()
    {
        var content = new StringContent(
            """{"title":"t","dueDate":"not a date"}""", System.Text.Encoding.UTF8, "application/json");

        await ReadProblemAsync(await _client.PostAsync("/tasks", content), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task WrongContentType_Returns415Problem()
    {
        var content = new StringContent("title=x", System.Text.Encoding.UTF8, "text/plain");

        await ReadProblemAsync(await _client.PostAsync("/tasks", content), HttpStatusCode.UnsupportedMediaType);
    }

    [Fact]
    public async Task OpenApiDocument_IsNotExposedInProduction()
    {
        // The container runs in Production, so the README promises the document is not served there.
        using var production = _factory.WithWebHostBuilder(b => b.UseEnvironment("Production"));
        using var client = production.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
