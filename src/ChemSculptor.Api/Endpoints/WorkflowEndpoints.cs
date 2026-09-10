using ChemSculptor.Core;
using ChemSculptor.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

public static class WorkflowEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder workflows = EndpointRouteBuilderExtensions.MapGroup(app, "/workflows");

        EndpointRouteBuilderExtensions.MapPost(workflows, "/", SubmitWorkflowAsync);
        EndpointRouteBuilderExtensions.MapGet(workflows, "/", ListWorkflows);
        EndpointRouteBuilderExtensions.MapGet(workflows, "/{id}", GetWorkflowAsync);
        EndpointRouteBuilderExtensions.MapPost(workflows, "/{id}/run", RunWorkflowAsync);
        EndpointRouteBuilderExtensions.MapPost(workflows, "/{id}/intervene", Intervene);
        EndpointRouteBuilderExtensions.MapGet(app, "/tasks/{workflowId}/log", GetTaskLogAsync);
        EndpointRouteBuilderExtensions.MapPost(app, "/approvals/{id}", Approve);

        return app;
    }

    private static async Task<IResult> SubmitWorkflowAsync(
        WorkflowDefinition definition,
        WorkflowEngine engine,
        CancellationToken cancellationToken)
    {
        try
        {
            WorkflowRun run = await engine.SubmitAsync(definition, cancellationToken);
            string location = "/workflows/" + run.Id;
            return Results.Created(location, run);
        }
        catch (InvalidOperationException ex)
        {
            Dictionary<string, string[]> errors = new Dictionary<string, string[]>();
            string[] messages = new string[1];
            messages[0] = ex.Message;
            errors.Add("definition", messages);
            return Results.ValidationProblem(errors);
        }
    }

    private static IResult ListWorkflows(IWorkflowRepository repository)
    {
        return Results.Ok(repository.List());
    }

    private static async Task<IResult> GetWorkflowAsync(
        string id,
        IWorkflowRepository repository,
        CancellationToken cancellationToken)
    {
        WorkflowRun? run = await repository.GetAsync(id, cancellationToken);

        if (run == null)
        {
            return Results.NotFound();
        }

        return Results.Ok(run);
    }

    private static async Task<IResult> RunWorkflowAsync(
        string id,
        WorkflowEngine engine,
        CancellationToken cancellationToken)
    {
        try
        {
            WorkflowRun run = await engine.RunAsync(id, cancellationToken);
            return Results.Ok(run);
        }
        catch (KeyNotFoundException ex)
        {
            ApiError error = new ApiError();
            error.Error = ex.Message;
            return Results.NotFound(error);
        }
    }

    private static IResult Intervene(string id, InterveneRequest request)
    {
        InterventionResponse response = new InterventionResponse();
        response.Id = id;
        response.Operation = request.Operation;
        response.NodeId = request.NodeId;
        response.Status = "queued";
        response.Note = "intervention hook is a framework stub";

        return Results.Ok(response);
    }

    private static async Task<IResult> GetTaskLogAsync(
        string workflowId,
        IWorkflowRepository repository,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<WorkflowEvent> events = await repository.GetEventsAsync(workflowId, cancellationToken);
        return Results.Ok(events);
    }

    private static IResult Approve(string id, ApprovalRequest request)
    {
        ApprovalResponse response = new ApprovalResponse();
        response.Id = id;
        response.Approved = request.Approved;
        response.Status = "recorded";
        response.Note = "approval hook is a framework stub";

        return Results.Ok(response);
    }
}
