using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace TaskTracker.Tests;

public class PaginationTests : IDisposable
{
    private readonly TaskTrackerFactory _factory = new();
    private readonly HttpClient _client;

    public PaginationTests() => _client = _factory.CreateClient();

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private async Task CreateManyAsync(int count)
    {
        for (var i = 1; i <= count; i++)
        {
            var response = await _client.PostAsJsonAsync("/tasks", new { title = $"task {i}" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }

    private static async Task<string[]> TitlesAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.EnumerateArray().Select(t => t.GetProperty("title").GetString()!).ToArray();
    }

    private static int TotalCount(HttpResponseMessage response) =>
        int.Parse(response.Headers.GetValues("X-Total-Count").Single());

    [Fact]
    public async Task List_WithoutParameters_ReturnsAtMost100AndReportsTheTotal()
    {
        await CreateManyAsync(105);

        var response = await _client.GetAsync("/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var titles = await TitlesAsync(response);
        Assert.Equal(100, titles.Length);
        Assert.Equal("task 1", titles[0]);
        Assert.Equal(105, TotalCount(response));
    }

    [Fact]
    public async Task List_WithLimitAndOffset_ReturnsThatSliceInIdOrder()
    {
        await CreateManyAsync(5);

        var response = await _client.GetAsync("/tasks?limit=2&offset=1");

        Assert.Equal(["task 2", "task 3"], await TitlesAsync(response));
        Assert.Equal(5, TotalCount(response));
    }

    [Fact]
    public async Task List_TotalCountFollowsTheStatusFilter()
    {
        await CreateManyAsync(3);
        await _client.PutAsJsonAsync("/tasks/1", new { title = "task 1", status = "Done" });

        var response = await _client.GetAsync("/tasks?status=Done&limit=1");

        Assert.Equal(["task 1"], await TitlesAsync(response));
        Assert.Equal(1, TotalCount(response));
    }

    [Fact]
    public async Task List_OffsetPastTheEnd_ReturnsAnEmptyListWithTheTotal()
    {
        await CreateManyAsync(2);

        var response = await _client.GetAsync("/tasks?offset=50");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await TitlesAsync(response));
        Assert.Equal(2, TotalCount(response));
    }

    [Theory]
    [InlineData("limit=0", "limit")]
    [InlineData("limit=501", "limit")]
    [InlineData("limit=-5", "limit")]
    [InlineData("offset=-1", "offset")]
    public async Task List_OutOfRangeParameters_Return400NamingTheField(string query, string field)
    {
        var response = await _client.GetAsync($"/tasks?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(doc.RootElement.GetProperty("errors").TryGetProperty(field, out _),
            $"Expected a validation error for '{field}'.");
    }

    [Theory]
    [InlineData("limit=abc")]
    [InlineData("offset=1.5")]
    public async Task List_NonNumericParameters_Return400(string query)
    {
        var response = await _client.GetAsync($"/tasks?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task LimitOf500_IsAccepted()
    {
        var response = await _client.GetAsync("/tasks?limit=500");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void TaskEndpoints_HaveA64KBRequestBodyLimit()
    {
        // The in-memory test server does not enforce body limits, so check the endpoint's
        // metadata, which real Kestrel enforces (a 70 KB body returns 413 there).
        var post = _factory.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(e => e.RoutePattern.RawText?.TrimEnd('/') == "/tasks"
                && e.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods.Contains("POST"));

        Assert.Equal(64 * 1024, post.Metadata.GetMetadata<IRequestSizeLimitMetadata>()?.MaxRequestBodySize);
    }
}
