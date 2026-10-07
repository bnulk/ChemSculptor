using ChemSculptor.ScientificData.Extraction.ArtifactResolution;
using ChemSculptor.ScientificData.Extraction.ArtifactResolution.Models;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

/// <summary>科学点文件成果包端点。</summary>
public static class ScientificArtifactEndpoints
{
    /// <summary>登记科学点文件成果包路由。</summary>
    public static IEndpointRouteBuilder MapScientificArtifactEndpoints(
        IEndpointRouteBuilder app)
    {
        EndpointRouteBuilderExtensions.MapGet(
            app,
            "/scientific-results/{resultId}/artifact-manifest",
            GetManifestByResultAsync);
        EndpointRouteBuilderExtensions.MapGet(
            app,
            "/scientific-results/{resultId}/artifacts/{pointId}/{artifactId}",
            DownloadArtifactAsync);
        EndpointRouteBuilderExtensions.MapGet(
            app,
            "/calculations/{jobId}/artifact-manifest",
            GetManifestByJobAsync);
        return app;
    }

    private static async Task<IResult> GetManifestByResultAsync(
        string resultId,
        IScientificArtifactResolver resolver,
        CancellationToken cancellationToken)
    {
        ScientificArtifactManifest manifest =
            await resolver.ResolveManifestAsync(
                resultId,
                cancellationToken);

        if (!manifest.Succeeded)
        {
            ApiError error = new ApiError();
            error.Error = manifest.Error;
            return Results.NotFound(error);
        }

        ScientificArtifactManifestResponse response =
            ScientificArtifactResponseMapper.Convert(manifest);
        return Results.Ok(response);
    }

    private static async Task<IResult> GetManifestByJobAsync(
        string jobId,
        IScientificDataRepository repository,
        IScientificArtifactResolver resolver,
        CancellationToken cancellationToken)
    {
        ScientificResult? result =
            ScientificResultLookup.FindByJobId(
                repository,
                jobId);

        if (result == null)
        {
            ApiError error = new ApiError();
            error.Error =
                "没有找到计算作业所属的科学成果：" + jobId;
            return Results.NotFound(error);
        }

        ScientificArtifactManifest manifest =
            await resolver.ResolveManifestAsync(
                result.Id,
                cancellationToken);

        if (!manifest.Succeeded)
        {
            ApiError error = new ApiError();
            error.Error = manifest.Error;
            return Results.NotFound(error);
        }

        ScientificArtifactManifestResponse response =
            ScientificArtifactResponseMapper.Convert(manifest);
        return Results.Ok(response);
    }

    private static async Task<IResult> DownloadArtifactAsync(
        string resultId,
        string pointId,
        string artifactId,
        IScientificArtifactResolver resolver,
        CancellationToken cancellationToken)
    {
        ScientificArtifactOpenResult openResult =
            await resolver.OpenArtifactAsync(
                resultId,
                pointId,
                artifactId,
                cancellationToken);

        if (!openResult.Succeeded
            || openResult.Artifact == null)
        {
            return ConvertOpenError(openResult.Error);
        }

        ScientificArtifactContent content =
            openResult.Artifact;
        string mediaType = content.MediaType;

        if (string.IsNullOrWhiteSpace(mediaType))
        {
            mediaType = "application/octet-stream";
        }

        return Results.File(
            content.Content,
            contentType: mediaType,
            fileDownloadName: content.DownloadFileName,
            enableRangeProcessing: true);
    }

    private static IResult ConvertOpenError(string error)
    {
        ApiError response = new ApiError();
        response.Error = error;

        if (error.Contains(
            "不存在",
            StringComparison.Ordinal))
        {
            return Results.NotFound(response);
        }

        if (error.Contains(
            "SHA-256",
            StringComparison.Ordinal))
        {
            return Results.Conflict(response);
        }

        return Results.BadRequest(response);
    }
}
