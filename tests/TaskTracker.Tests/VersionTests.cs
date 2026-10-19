using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting;
using TaskTracker.Api;

namespace TaskTracker.Tests;

public class VersionTests : IDisposable
{
    private readonly TaskTrackerFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetVersion_ReturnsSemanticVersion()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync("/version");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var version = doc.RootElement.GetProperty("version").GetString();
        Assert.Equal(AppVersion.Current, version);
        Assert.Matches(new Regex(@"^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$"), version);
    }

    [Fact]
    public async Task OpenApiDocument_UsesTheAppVersion()
    {
        // The OpenAPI endpoint is only mapped in Development.
        using var devFactory = _factory.WithWebHostBuilder(b => b.UseEnvironment("Development"));
        using var client = devFactory.CreateClient();

        var response = await client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(AppVersion.Current, doc.RootElement.GetProperty("info").GetProperty("version").GetString());
    }
}
