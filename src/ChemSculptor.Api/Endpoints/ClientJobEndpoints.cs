using ChemSculptor.Api.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

public static class ClientJobEndpoints
{
    public static IEndpointRouteBuilder MapClientJobEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder jobs = EndpointRouteBuilderExtensions.MapGroup(app, "/client/jobs");

        EndpointRouteBuilderExtensions.MapPost(jobs, "/", UploadClientJobAsync);
        EndpointRouteBuilderExtensions.MapGet(jobs, "/{id}/status", GetClientJobStatus);
        EndpointRouteBuilderExtensions.MapGet(jobs, "/{id}/result", GetClientJobResult);

        return app;
    }

    private static async Task<IResult> UploadClientJobAsync(
        HttpRequest request,
        ClientJobService service,
        CancellationToken cancellationToken)
    {
        string rawText;

        using (StreamReader reader = new StreamReader(request.Body))
        {
            rawText = await reader.ReadToEndAsync(cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(rawText))
        {
            ApiError error = new ApiError();
            error.Error = "请上传非空的 txt 文本。";
            return Results.BadRequest(error);
        }

        ClientJob job = await service.SubmitAsync(rawText, cancellationToken);

        ClientJobAcceptedResponse response = new ClientJobAcceptedResponse();
        response.Id = job.Id;
        response.Status = job.Status;
        response.Message = job.Message;

        string location = "/client/jobs/" + job.Id;
        return Results.Accepted(location, response);
    }

    private static IResult GetClientJobStatus(string id, ClientJobService service)
    {
        ClientJob? job = service.GetJob(id);

        if (job == null)
        {
            ApiError error = new ApiError();
            error.Error = "任务 " + id + " 不存在。";
            return Results.NotFound(error);
        }

        ClientJobStatusResponse response = new ClientJobStatusResponse();
        response.Id = job.Id;
        response.Status = job.Status;
        response.Message = job.Message;
        response.CreatedAt = job.CreatedAt;
        response.StartedAt = job.StartedAt;
        response.CompletedAt = job.CompletedAt;
        response.HasResult = job.ResultText != null;

        return Results.Ok(response);
    }

    private static IResult GetClientJobResult(string id, ClientJobService service)
    {
        ClientJob? job = service.GetJob(id);

        if (job == null)
        {
            ApiError error = new ApiError();
            error.Error = "任务 " + id + " 不存在。";
            return Results.NotFound(error);
        }

        if (job.ResultText == null)
        {
            ApiError error = new ApiError();
            error.Error = "结果尚未就绪。";
            return Results.Conflict(error);
        }

        return Results.Text(job.ResultText, "text/plain; charset=utf-8");
    }
}
