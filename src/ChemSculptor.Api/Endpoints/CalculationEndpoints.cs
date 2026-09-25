using ChemSculptor.Agent;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace ChemSculptor.Api;

/// <summary>
/// 计算相关端点。
/// 只负责把 HTTP 请求交给智能体编排层。
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
        IAgentService agentService,
        CancellationToken cancellationToken)
    {
        AgentSinglePointRequest agentRequest = new AgentSinglePointRequest();
        agentRequest.CoordinateText = request.CoordinateText;

        AgentResult result = await agentService.ExecuteSinglePointAsync(
            agentRequest,
            cancellationToken);

        if (!result.IsSupported)
        {
            CalculationErrorResponse error = new CalculationErrorResponse();
            error.Error = result.Error;
            error.Diagnostics = new List<string>(result.Diagnostics);
            return Results.BadRequest(error);
        }

        SinglePointCalculationResponse response = new SinglePointCalculationResponse();
        response.JobId = result.JobId;
        response.Status = result.Status;
        response.InputFilePath = result.InputFilePath;
        response.Message = result.Message;

        return Results.Ok(response);
    }
}
