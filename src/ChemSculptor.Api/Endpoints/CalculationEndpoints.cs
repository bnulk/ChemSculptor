using System.Globalization;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.InputProcessor;
using ChemSculptor.InputProcessor.GeometryIntake;
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

    /// <summary>
    /// 触发单点计算调试流程。
    /// 解析坐标、创建工作区并生成输入文件，但不运行计算程序。
    /// </summary>
    private static async Task<IResult> SubmitSinglePointAsync(
        SinglePointCalculationRequest request,
        IGeometryTextParser geometryParser,
        ICalculationWorkspace workspace,
        GaussianInputWriter inputWriter,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.CoordinateText))
        {
            CalculationErrorResponse emptyError = new CalculationErrorResponse();
            emptyError.Error = "坐标文本不能为空。";
            return Results.BadRequest(emptyError);
        }

        MolecularGeometry molecularGeometry =
            await geometryParser.ParseAsync(request.CoordinateText, cancellationToken);

        if (molecularGeometry.Atoms.Count == 0)
        {
            CalculationErrorResponse geometryError = new CalculationErrorResponse();
            geometryError.Error = "未能从坐标文本中解析出原子。";
            geometryError.Diagnostics = new List<string>(molecularGeometry.Diagnostics);
            return Results.BadRequest(geometryError);
        }

        string jobId = "job-" + Guid.NewGuid().ToString("N");
        await workspace.EnsureJobWorkspaceAsync(jobId, cancellationToken);

        string coordinatePath = Path.Combine(
            workspace.GetInputDirectory(jobId),
            CalculationWorkspacePaths.JobCoordinatesFileName);
        await File.WriteAllTextAsync(coordinatePath, request.CoordinateText, cancellationToken);

        CanonicalGeometry canonicalGeometry =
            CanonicalGeometryMapper.FromMolecularGeometry(molecularGeometry, jobId);

        CalculationSpec spec = CalculationDefaults.CreateDefaultSinglePoint();
        spec.Charge = request.Charge;
        spec.Multiplicity = request.Multiplicity;

        SetParameterValue(spec, "charge", request.Charge.ToString(CultureInfo.InvariantCulture));
        SetParameterValue(
            spec,
            "multiplicity",
            request.Multiplicity.ToString(CultureInfo.InvariantCulture));

        string inputFileName = jobId + ".gjf";
        string inputPath = Path.Combine(workspace.GetInputDirectory(jobId), inputFileName);

        GaussianInputOptions inputOptions = new GaussianInputOptions();
        inputOptions.Memory = "4GB";
        inputOptions.ProcessorCount = CalculationDefaults.DefaultProcessorCount;
        inputOptions.CheckpointFilePath = Path.ChangeExtension(inputPath, ".chk");
        inputOptions.Title = "ChemSculptor single point calculation";

        await inputWriter.WriteAsync(
            spec,
            canonicalGeometry,
            inputOptions,
            inputPath,
            cancellationToken);

        SinglePointCalculationResponse response = new SinglePointCalculationResponse();
        response.JobId = jobId;
        response.Status = CalculationJobState.InputGenerated.ToString();
        response.InputFilePath = inputPath;
        response.Message = "Gaussian 输入文件已生成，尚未启动计算程序。";

        return Results.Ok(response);
    }

    /// <summary>更新计算参数当前值与来源。</summary>
    private static void SetParameterValue(
        CalculationSpec spec,
        string parameterName,
        string value)
    {
        for (int index = 0; index < spec.Parameters.Count; index++)
        {
            CalculationParameter parameter = spec.Parameters[index];

            if (string.Equals(parameter.Name, parameterName, StringComparison.OrdinalIgnoreCase))
            {
                parameter.CurrentValue = value;
                parameter.Source = ParameterSource.User;
            }
        }
    }
}
