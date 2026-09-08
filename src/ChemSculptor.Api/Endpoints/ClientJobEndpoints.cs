using ChemSculptor.Api.Client;
using Microsoft.AspNetCore.Mvc;

namespace ChemSculptor.Api;

public static class ClientJobEndpoints
{
    public static IEndpointRouteBuilder MapClientJobEndpoints(this IEndpointRouteBuilder app)
    {
        var jobs = app.MapGroup("/client/jobs");

        // 语法糖说明：async (参数) => { } 是“异步 Lambda（匿名方法）”。
        // 传统写法是定义一个命名方法再传进来，例如：
        // jobs.MapPost("/", UploadHandler);
        // 而 UploadHandler 是一个带相同参数、返回 Task 的方法。
        jobs.MapPost("/", async (
            [FromForm] IFormFile file,
            ClientJobService service,
            CancellationToken ct) =>
        {
            if (file is null || file.Length == 0)
            {
                return Results.BadRequest(new { error = "请上传一个非空的 txt 文件。" });
            }

            // 语法糖说明：await using 等价于在作用域结束时调用
            // await fileStream.DisposeAsync()（异步释放），
            // 类似普通 using 结束时的 fileStream.Dispose()。
            await using var stream = file.OpenReadStream();
            var job = await service.SubmitAsync(stream, ct);
            // 语法糖说明：new { ... } 是匿名类型。编译器会生成一个只读类，
            // 等价于手工定义 class UploadAccepted { Id; Status; Message; }
            // 再 new 一个实例。
            return Results.Accepted(
                $"/client/jobs/{job.Id}",
                new { job.Id, job.Status, job.Message });
        }).DisableAntiforgery();

        jobs.MapGet("/{id}/status", (string id, ClientJobService service) =>
        {
            var job = service.GetJob(id);
            // 语法糖说明：? : 是三元运算符，等价于：
            // if (job is null) { return Results.NotFound(...); }
            // else { return Results.Ok(...); }
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

            // 语法糖说明：job.ResultText is null 等价于
            // job.ResultText == null，是更明确的“模式匹配”写法。
            if (job.ResultText is null)
            {
                return Results.Conflict(new { error = "结果尚未就绪。" });
            }

            return Results.Text(job.ResultText, "text/plain; charset=utf-8");
        });

        return app;
    }
}
