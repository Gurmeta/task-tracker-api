namespace TaskTracker.Api.Endpoints;

public static class VersionEndpoints
{
    public sealed record VersionResponse(string Version);

    public static IEndpointRouteBuilder MapVersionEndpoint(this IEndpointRouteBuilder routes)
    {
        routes.MapGet("/version", () => Results.Ok(new VersionResponse(AppVersion.Current)))
            .WithTags("Meta")
            .WithSummary("Get the running service version")
            .Produces<VersionResponse>(StatusCodes.Status200OK);

        return routes;
    }
}
