using ChemSculptor.Agent;
using ChemSculptor.Compute;
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
        EndpointRouteBuilderExtensions.MapGet(calculations, "/{jobId}/status", GetCalculationStatusAsync);
        EndpointRouteBuilderExtensions.MapGet(calculations, "/{jobId}/result", GetCalculationResultAsync);
        EndpointRouteBuilderExtensions.MapGet(calculations, "/{jobId}/validation", GetCalculationValidationAsync);

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
        response.OutputFilePath = result.OutputFilePath;
        response.Message = result.Message;

        return Results.Ok(response);
    }

    /// <summary>查询计算作业状态。</summary>
    private static async Task<IResult> GetCalculationStatusAsync(
        string jobId,
        ICalculationQueryService queryService,
        CancellationToken cancellationToken)
    {
        CalculationJob? job = await queryService.GetJobAsync(jobId, cancellationToken);

        if (job == null)
        {
            ApiError error = new ApiError();
            error.Error = "计算作业 " + jobId + " 不存在。";
            return Results.NotFound(error);
        }

        CalculationStatusResponse response = new CalculationStatusResponse();
        response.JobId = job.JobId;
        response.State = job.State.ToString();
        response.StartedAt = job.StartedAt;
        response.CompletedAt = job.CompletedAt;
        response.InputFilePath = job.InputFilePath;
        response.OutputFilePath = job.OutputFilePath;
        response.Diagnostics = ConvertDiagnostics(job.Diagnostics);

        return Results.Ok(response);
    }

    /// <summary>查询规范化计算结果。</summary>
    private static async Task<IResult> GetCalculationResultAsync(
        string jobId,
        ICalculationQueryService queryService,
        CancellationToken cancellationToken)
    {
        CalculationJob? job = await queryService.GetJobAsync(jobId, cancellationToken);

        if (job == null)
        {
            ApiError error = new ApiError();
            error.Error = "计算作业 " + jobId + " 不存在。";
            return Results.NotFound(error);
        }

        CalculationResult? result = await queryService.GetResultAsync(
            jobId,
            cancellationToken);

        if (result == null)
        {
            ApiError error = new ApiError();
            error.Error = "计算结果尚未就绪。";
            return Results.Conflict(error);
        }

        CalculationResultResponse response = new CalculationResultResponse();
        response.JobId = result.JobId;
        response.Energy = result.Energy;
        response.EnergyUnit = result.EnergyUnit;
        response.NormalTermination = result.NormalTermination;
        response.FailureKind = result.FailureKind.ToString();
        response.Program = result.Program;
        response.Method = result.Method;
        response.Basis = result.Basis;
        response.Charge = result.Charge;
        response.Multiplicity = result.Multiplicity;
        response.OutputFilePath = result.OutputFilePath;
        response.Diagnostics = ConvertDiagnostics(result.Diagnostics);

        return Results.Ok(response);
    }

    /// <summary>查询计算结果验证报告。</summary>
    private static async Task<IResult> GetCalculationValidationAsync(
        string jobId,
        ICalculationQueryService queryService,
        CancellationToken cancellationToken)
    {
        CalculationJob? job = await queryService.GetJobAsync(jobId, cancellationToken);

        if (job == null)
        {
            ApiError error = new ApiError();
            error.Error = "计算作业 " + jobId + " 不存在。";
            return Results.NotFound(error);
        }

        CalculationValidationReport? validation =
            await queryService.GetValidationAsync(jobId, cancellationToken);

        if (validation == null)
        {
            ApiError error = new ApiError();
            error.Error = "计算结果验证报告尚未就绪。";
            return Results.Conflict(error);
        }

        CalculationValidationResponse response = new CalculationValidationResponse();
        response.JobId = jobId;
        response.Passed = validation.Passed;
        response.Status = validation.Status.ToString();
        response.Summary = validation.Summary;
        response.ValidatorName = validation.ValidatorName;
        response.ValidatedAt = validation.ValidatedAt;
        response.Checks = ConvertValidationChecks(validation.Checks);
        response.Issues = ConvertValidationIssues(validation.Issues);
        return Results.Ok(response);
    }

    private static List<CalculationValidationCheckResponse> ConvertValidationChecks(
        List<CalculationValidationCheck> checks)
    {
        List<CalculationValidationCheckResponse> responses =
            new List<CalculationValidationCheckResponse>();

        for (int index = 0; index < checks.Count; index++)
        {
            CalculationValidationCheck check = checks[index];
            CalculationValidationCheckResponse response =
                new CalculationValidationCheckResponse();
            response.Code = check.Code;
            response.Description = check.Description;
            response.Passed = check.Passed;
            response.Severity = check.Severity.ToString();
            response.Requirement = check.Requirement.ToString();
            response.Scope = check.Scope.ToString();
            response.ExpectedValue = check.ExpectedValue;
            response.ActualValue = check.ActualValue;
            response.Message = check.Message;
            responses.Add(response);
        }

        return responses;
    }

    private static List<CalculationDiagnosticResponse> ConvertValidationIssues(
        List<CalculationValidationIssue> issues)
    {
        List<CalculationDiagnosticResponse> responses =
            new List<CalculationDiagnosticResponse>();

        for (int index = 0; index < issues.Count; index++)
        {
            CalculationValidationIssue issue = issues[index];
            CalculationDiagnosticResponse response =
                new CalculationDiagnosticResponse();
            response.Severity = issue.Severity.ToString();
            response.Code = issue.Code;
            response.Message = issue.Message;
            responses.Add(response);
        }

        return responses;
    }

    private static List<CalculationDiagnosticResponse> ConvertDiagnostics(
        List<CalculationDiagnostic> diagnostics)
    {
        List<CalculationDiagnosticResponse> responses =
            new List<CalculationDiagnosticResponse>();

        for (int index = 0; index < diagnostics.Count; index++)
        {
            CalculationDiagnostic diagnostic = diagnostics[index];
            CalculationDiagnosticResponse response = new CalculationDiagnosticResponse();
            response.Severity = diagnostic.Severity.ToString();
            response.Code = diagnostic.Code;
            response.Message = diagnostic.Message;
            responses.Add(response);
        }

        return responses;
    }
}
