using ChemSculptor.ScientificData.Extraction.ArtifactResolution.Backfill;
using ChemSculptor.ScientificData.Extraction.ArtifactResolution.Backfill.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

/// <summary>旧科学数据文件引用回填端点。</summary>
public static class ScientificArtifactBackfillEndpoints
{
    /// <summary>登记文件引用回填路由。</summary>
    public static IEndpointRouteBuilder
        MapScientificArtifactBackfillEndpoints(
            IEndpointRouteBuilder app)
    {
        EndpointRouteBuilderExtensions.MapPost(
            app,
            "/scientific-results/{resultId}/artifact-backfill",
            BackfillByResultAsync);
        EndpointRouteBuilderExtensions.MapPost(
            app,
            "/scientific-artifacts/backfill",
            BackfillAllAsync);
        return app;
    }

    private static async Task<IResult> BackfillByResultAsync(
        string resultId,
        IScientificArtifactBackfillService service,
        CancellationToken cancellationToken)
    {
        ScientificArtifactBackfillResult result =
            await service.BackfillAsync(
                resultId,
                cancellationToken);

        if (!result.Succeeded)
        {
            ApiError error = new ApiError();
            error.Error = result.Error;
            return Results.NotFound(error);
        }

        return Results.Ok(result);
    }

    private static async Task<IResult> BackfillAllAsync(
        IScientificArtifactBackfillService service,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ScientificArtifactBackfillResult> results =
            await service.BackfillAllAsync(
                cancellationToken);
        return Results.Ok(results);
    }
}
