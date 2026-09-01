using ChemSculptor.Core;
using ChemSculptor.Domain;

namespace ChemSculptor.Api;

public static class WorkflowEndpoints
{
    public static IEndpointRouteBuilder MapWorkflowEndpoints(this IEndpointRouteBuilder app)
    {
        var workflows = app.MapGroup("/workflows");

        workflows.MapPost("/", async (WorkflowDefinition definition, WorkflowEngine engine, CancellationToken ct) =>
        {
            try
            {
                var run = await engine.SubmitAsync(definition, ct);
                return Results.Created($"/workflows/{run.Id}", run);
            }
            catch (InvalidOperationException ex)
            {
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { ["definition"] = [ex.Message] });
            }
        });

        workflows.MapGet("/", (IWorkflowRepository repository) => Results.Ok(repository.List()));

        workflows.MapGet("/{id}", async (string id, IWorkflowRepository repository, CancellationToken ct) =>
        {
            var run = await repository.GetAsync(id, ct);
            return run is null ? Results.NotFound() : Results.Ok(run);
        });

        workflows.MapPost("/{id}/run", async (string id, WorkflowEngine engine, CancellationToken ct) =>
        {
            try
            {
                var run = await engine.RunAsync(id, ct);
                return Results.Ok(run);
            }
            catch (KeyNotFoundException ex)
            {
                return Results.NotFound(new { error = ex.Message });
            }
        });

        workflows.MapPost("/{id}/intervene", (string id, InterveneRequest request) =>
            Results.Ok(new
            {
                id,
                request.Operation,
                request.NodeId,
                Status = "queued",
                Note = "intervention hook is a framework stub"
            }));

        app.MapGet("/tasks/{workflowId}/log",
            async (string workflowId, IWorkflowRepository repository, CancellationToken ct) =>
                Results.Ok(await repository.GetEventsAsync(workflowId, ct)));

        app.MapPost("/approvals/{id}", (string id, ApprovalRequest request) =>
            Results.Ok(new
            {
                id,
                request.Approved,
                Status = "recorded",
                Note = "approval hook is a framework stub"
            }));

        return app;
    }
}
