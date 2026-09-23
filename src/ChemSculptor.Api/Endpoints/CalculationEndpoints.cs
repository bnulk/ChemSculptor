using ChemSculptor.Compute;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

/// <summary>
/// 计算相关端点。
/// 当前阶段只触发到“生成输入文件”，不启动计算程序。
/// </summary>
public static class CalculationEndpoints
{
    /// <summary>登记计算相关路由。</summary>
    public static IEndpointRouteBuilder MapCalculationEndpoints(IEndpointRouteBuilder app)
    {
        RouteGroupBuilder calculations = EndpointRouteBuilderExtensions.MapGroup(app, "/calculations");

        EndpointRouteBuilderExtensions.MapPost(calculations, "/single-point", SubmitSinglePointAsync);

        return app;
    }

    /// <summary>直接触发的单点计算调试端点。</summary>
    private static async Task<IResult> SubmitSinglePointAsync(
        SinglePointCalculationRequest request,
        SinglePointCalculationExecutor executor,
        CancellationToken cancellationToken)
    {
        SinglePointExecutionResult executionResult = await executor.ExecuteAsync(
            request.CoordinateText,
            request.Charge,
            request.Multiplicity,
            cancellationToken);

        if (!executionResult.Succeeded)
        {
            CalculationErrorResponse error = new CalculationErrorResponse();
            error.Error = executionResult.Error;
            error.Diagnostics = new List<string>(executionResult.Diagnostics);
            return Results.BadRequest(error);
        }

        SinglePointCalculationResponse response = new SinglePointCalculationResponse();
        response.JobId = executionResult.JobId;
        response.Status = executionResult.Status;
        response.InputFilePath = executionResult.InputFilePath;
        response.Message = executionResult.Message;

        return Results.Ok(response);
    }
}
