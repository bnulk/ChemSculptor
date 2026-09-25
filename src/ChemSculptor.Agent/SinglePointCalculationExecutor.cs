using System.Globalization;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.InputProcessor;
using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Agent;

/// <summary>单点计算调试执行结果。</summary>
public sealed class SinglePointExecutionResult
{
    /// <summary>是否成功。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();

    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>当前状态。</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>生成的输入文件路径。</summary>
    public string InputFilePath { get; set; } = string.Empty;

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 单点计算执行器。
/// 负责解析坐标、创建工作区和生成输入文件，不启动计算程序。
/// </summary>
public sealed class SinglePointCalculationExecutor
{
    private readonly IGeometryTextParser _geometryParser;
    private readonly ICalculationWorkspace _workspace;
    private readonly GaussianInputWriter _inputWriter;

    /// <summary>创建执行器。</summary>
    public SinglePointCalculationExecutor(
        IGeometryTextParser geometryParser,
        ICalculationWorkspace workspace,
        GaussianInputWriter inputWriter)
    {
        _geometryParser = geometryParser;
        _workspace = workspace;
        _inputWriter = inputWriter;
    }

    /// <summary>执行单点计算流程。</summary>
    public async Task<SinglePointExecutionResult> ExecuteAsync(
        string coordinateText,
        int charge,
        int multiplicity,
        CancellationToken cancellationToken)
    {
        SinglePointExecutionResult result = new SinglePointExecutionResult();

        if (string.IsNullOrWhiteSpace(coordinateText))
        {
            result.Succeeded = false;
            result.Error = "坐标文本不能为空。";
            return result;
        }

        try
        {
            MolecularGeometry molecularGeometry =
                await _geometryParser.ParseAsync(coordinateText, cancellationToken);

            if (molecularGeometry.Atoms.Count == 0)
            {
                result.Succeeded = false;
                result.Error = "未能从坐标文本中解析出原子。";
                result.Diagnostics = new List<string>(molecularGeometry.Diagnostics);
                return result;
            }

            string jobId = "job-" + Guid.NewGuid().ToString("N");
            await _workspace.EnsureJobWorkspaceAsync(jobId, cancellationToken);

            string coordinatePath = Path.Combine(
                _workspace.GetInputDirectory(jobId),
                CalculationWorkspacePaths.JobCoordinatesFileName);
            await File.WriteAllTextAsync(coordinatePath, coordinateText, cancellationToken);

            CanonicalGeometry canonicalGeometry =
                CanonicalGeometryMapper.FromMolecularGeometry(molecularGeometry, jobId);

            CalculationSpec spec = CalculationDefaults.CreateDefaultSinglePoint();
            spec.Charge = charge;
            spec.Multiplicity = multiplicity;

            SetParameterValue(spec, "charge", charge.ToString(CultureInfo.InvariantCulture));
            SetParameterValue(
                spec,
                "multiplicity",
                multiplicity.ToString(CultureInfo.InvariantCulture));

            string inputFileName = jobId + ".gjf";
            string inputPath = Path.Combine(_workspace.GetInputDirectory(jobId), inputFileName);

            GaussianInputOptions inputOptions = new GaussianInputOptions();
            inputOptions.Memory = "4GB";
            inputOptions.ProcessorCount = CalculationDefaults.DefaultProcessorCount;
            inputOptions.CheckpointFilePath = Path.ChangeExtension(inputPath, ".chk");
            inputOptions.Title = "ChemSculptor single point calculation";

            await _inputWriter.WriteAsync(
                spec,
                canonicalGeometry,
                inputOptions,
                inputPath,
                cancellationToken);

            result.Succeeded = true;
            result.JobId = jobId;
            result.Status = CalculationJobState.InputGenerated.ToString();
            result.InputFilePath = inputPath;
            result.Message = "Gaussian 输入文件已生成，尚未启动计算程序。";

            return result;
        }
        catch (Exception ex)
        {
            result.Succeeded = false;
            result.Error = ex.Message;
            return result;
        }
    }

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
