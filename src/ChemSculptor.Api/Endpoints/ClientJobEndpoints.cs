using ChemSculptor.Api.Client;
using Microsoft.AspNetCore.Mvc;

namespace ChemSculptor.Api;

public static class ClientJobEndpoints
{
    public static IEndpointRouteBuilder MapClientJobEndpoints(this IEndpointRouteBuilder app)
    {
        var jobs = app.MapGroup("/client/jobs");

        jobs.MapPost("/", async (
            [FromForm] IFormFile file,
            ClientJobService service,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "请上传一个非空的 txt 文件。" });
            }

            await using var stream = file.OpenReadStream();
            var job = await service.SubmitAsync(stream, ct);
            return Results.Accepted(
                $"/client/jobs/{job.Id}",
                new { job.Id, job.Status, job.Message });
        });

        jobs.MapGet("/{id}/status", (string id, ClientJobService service) =>
        {
            var job = service.GetJob(id);
            return job is null
                ? Results.NotFound(new { error = $"任务 {id} 不存在。" })
                : Results.Ok(new
                {
                    job.Id,
                    job.Status,
                    job.Message,
                    job.CreatedAt,
                    job.StartedAt,
                    job.CompletedAt,
                    HasResult = job.ResultText is not null
                });
        });

        jobs.MapGet("/{id}/result", (string id, ClientJobService service) =>
        {
            var job = service.GetJob(id);
            if (job is null)
            {
                return Results.NotFound(new { error = $"任务 {id} 不存在。" });
            }

            if (job.ResultText is null)
            {
                return Results.Conflict(new { error = "结果尚未就绪。" });
            }

            return Results.Text(job.ResultText, "text/plain; charset=utf-8");
        });

        return app;
    }
}
