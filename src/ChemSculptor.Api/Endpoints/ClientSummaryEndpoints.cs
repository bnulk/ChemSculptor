using ChemSculptor.Api.Client;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;
using ChemSculptor.ScientificSummary.Abstractions;
using ChemSculptor.ScientificSummary.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

/// <summary>客户端科学摘要端点。</summary>
public static class ClientSummaryEndpoints
{
    /// <summary>登记客户端摘要路由。</summary>
    public static IEndpointRouteBuilder MapClientSummaryEndpoints(
        IEndpointRouteBuilder app)
    {
        EndpointRouteBuilderExtensions.MapGet(
            app,
            "/scientific-results/{resultId}/client-summary",
            BuildSummaryAsync);
        EndpointRouteBuilderExtensions.MapGet(
            app,
            "/calculations/{jobId}/client-summary",
            BuildSummaryByJobAsync);
        EndpointRouteBuilderExtensions.MapPost(
            app,
            "/scientific-results/{resultId}/client-summary/send/{clientId}",
            SendSummaryAsync);
        EndpointRouteBuilderExtensions.MapPost(
            app,
            "/calculations/{jobId}/client-summary/send/{clientId}",
            SendSummaryByJobAsync);
        EndpointRouteBuilderExtensions.MapGet(
            app,
            "/clients/{clientId}/scientific-summaries",
            TakeSummaries);
        return app;
    }

    private static async Task<IResult> BuildSummaryByJobAsync(
        string jobId,
        IScientificDataRepository repository,
        IClientSummaryBuilder builder,
        CancellationToken cancellationToken)
    {
        ScientificResult? result = ResolveResult(
            repository,
            jobId);

        if (result == null)
        {
            return Results.NotFound();
        }

        ClientScientificSummary summary =
            await builder.BuildAsync(
                result,
                cancellationToken);
        return Results.Ok(summary);
    }

    private static async Task<IResult> BuildSummaryAsync(
        string resultId,
        IScientificDataRepository repository,
        IClientSummaryBuilder builder,
        CancellationToken cancellationToken)
    {
        ScientificResult? result =
            await repository.GetAsync(
                resultId,
                cancellationToken);

        if (result == null)
        {
            return Results.NotFound();
        }

        ClientScientificSummary summary =
            await builder.BuildAsync(
                result,
                cancellationToken);
        return Results.Ok(summary);
    }

    private static async Task<IResult> SendSummaryAsync(
        string resultId,
        string clientId,
        IClientSummaryService service,
        CancellationToken cancellationToken)
    {
        ClientScientificSummary? summary =
            await service.BuildAndSendAsync(
                resultId,
                clientId,
                cancellationToken);

        if (summary == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(summary);
    }

    private static IResult TakeSummaries(
        string clientId,
        ApiClientSummarySender sender)
    {
        return Results.Ok(sender.Take(clientId));
    }

    private static async Task<IResult> SendSummaryByJobAsync(
        string jobId,
        string clientId,
        IScientificDataRepository repository,
        IClientSummaryService service,
        CancellationToken cancellationToken)
    {
        ScientificResult? result = ResolveResult(
            repository,
            jobId);

        if (result == null)
        {
            return Results.NotFound();
        }

        ClientScientificSummary? summary =
            await service.BuildAndSendAsync(
                result.Id,
                clientId,
                cancellationToken);

        if (summary == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(summary);
    }

    private static ScientificResult? ResolveResult(
        IScientificDataRepository repository,
        string jobId)
    {
        return ScientificResultLookup.FindByJobId(
            repository,
            jobId);
    }
}
