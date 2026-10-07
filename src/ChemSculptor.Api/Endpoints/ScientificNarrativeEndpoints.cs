using System.Text.Json;
using System.Text.Json.Serialization;
using ChemSculptor.ScientificData.Storage;
using ChemSculptor.ScientificSummary.Abstractions;
using ChemSculptor.ScientificSummary.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

/// <summary>科学数据叙述文本端点。</summary>
public static class ScientificNarrativeEndpoints
{
    private static readonly JsonSerializerOptions NarrativeJsonOptions =
        CreateJsonOptions();

    /// <summary>登记科学数据叙述文本路由。</summary>
    public static IEndpointRouteBuilder MapScientificNarrativeEndpoints(
        IEndpointRouteBuilder app)
    {
        EndpointRouteBuilderExtensions.MapGet(
            app,
            "/scientific-results/{resultId}/narrative",
            GetByResultAsync);
        EndpointRouteBuilderExtensions.MapGet(
            app,
            "/calculations/{jobId}/narrative",
            GetByJobAsync);
        return app;
    }

    private static async Task<IResult> GetByResultAsync(
        string resultId,
        IScientificNarrativeService service,
        CancellationToken cancellationToken)
    {
        ScientificNarrativePackage? package =
            await service.BuildAsync(
                resultId,
                cancellationToken);

        if (package == null)
        {
            ApiError error = new ApiError();
            error.Error =
                "没有找到科学成果：" + resultId;
            return Results.NotFound(error);
        }

        return Results.Json(
            package,
            NarrativeJsonOptions);
    }

    private static async Task<IResult> GetByJobAsync(
        string jobId,
        IScientificDataRepository repository,
        IScientificNarrativeService service,
        CancellationToken cancellationToken)
    {
        ChemSculptor.ScientificData.Models.ScientificResult? result =
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

        ScientificNarrativePackage? package =
            await service.BuildAsync(
                result.Id,
                cancellationToken);

        if (package == null)
        {
            ApiError error = new ApiError();
            error.Error =
                "没有找到科学成果：" + result.Id;
            return Results.NotFound(error);
        }

        return Results.Json(
            package,
            NarrativeJsonOptions);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options =
            new JsonSerializerOptions(
                JsonSerializerDefaults.Web);
        options.Converters.Add(
            new JsonStringEnumConverter());
        return options;
    }
}
