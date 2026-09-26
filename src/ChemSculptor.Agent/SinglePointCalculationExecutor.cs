using ChemSculptor.Compute;
using ChemSculptor.InputProcessor;
using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Agent;

/// <summary>单点计算执行结果。</summary>
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

    /// <summary>计算输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 单点计算执行器。
/// 负责解析坐标、创建工作区、生成输入文件，并把作业提交给计算后端。
/// </summary>
public sealed class SinglePointCalculationExecutor
{
    private readonly IGeometryTextParser _geometryParser;
    private readonly ICalculationWorkspace _workspace;
    private readonly IQuantumProgramAdapter _programAdapter;
    private readonly IComputeBackend _computeBackend;

    /// <summary>创建执行器。</summary>
    public SinglePointCalculationExecutor(
        IGeometryTextParser geometryParser,
        ICalculationWorkspace workspace,
        IQuantumProgramAdapter programAdapter,
        IComputeBackend computeBackend)
    {
        _geometryParser = geometryParser;
        _workspace = workspace;
        _programAdapter = programAdapter;
        _computeBackend = computeBackend;
    }

    /// <summary>执行单点计算流程。</summary>
    public async Task<SinglePointExecutionResult> ExecuteAsync(
        string coordinateText,
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

            // 化学参数由服务器端默认方案提供，当前不使用客户端参数。
            CalculationSpec spec = CalculationDefaults.CreateDefaultSinglePoint();

            if (!_programAdapter.CanRun(spec))
            {
                result.Succeeded = false;
                result.Error = "没有可处理 " + spec.Program + " 的计算程序适配器。";
                return result;
            }

            string inputFileName = jobId + ".gjf";
            string inputPath = Path.Combine(_workspace.GetInputDirectory(jobId), inputFileName);
            string runInputPath = Path.Combine(_workspace.GetRunDirectory(jobId), inputFileName);
            string outputPath = _workspace.GetJobOutputPath(jobId);

            await _programAdapter.WriteInputAsync(
                spec,
                canonicalGeometry,
                inputPath,
                cancellationToken);

            File.Copy(inputPath, runInputPath, true);

            CalculationJob job = new CalculationJob();
            job.JobId = jobId;
            job.Spec = spec;
            job.State = CalculationJobState.Created;
            job.WorkspaceDirectory = _workspace.GetJobDirectory(jobId);
            job.RunDirectory = _workspace.GetRunDirectory(jobId);
            job.InputFilePath = runInputPath;
            job.OutputFilePath = outputPath;

            job.State = CalculationJobState.InputGenerated;

            CalculationExecutionContext context =
                _programAdapter.BuildExecutionContext(job, spec);

            await _computeBackend.SubmitAsync(job, context, cancellationToken);

            job.State = CalculationJobState.Running;

            result.Succeeded = true;
            result.JobId = jobId;
            result.Status = CalculationJobState.Running.ToString();
            result.InputFilePath = inputPath;
            result.OutputFilePath = outputPath;
            result.Message = spec.Program + " 输入文件已复制到运行目录，计算已在后台启动。";

            return result;
        }
        catch (Exception ex)
        {
            result.Succeeded = false;
            result.Error = ex.Message;
            return result;
        }
    }

}
