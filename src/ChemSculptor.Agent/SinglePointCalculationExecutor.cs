using ChemSculptor.Compute;

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
    private readonly ICalculationWorkspace _workspace;
    private readonly IComputeBackend _computeBackend;
    private readonly ICalculationRepository _repository;
    private readonly ICalculationJobMonitor _jobMonitor;
    private readonly ISkillInvoker _skillInvoker;

    /// <summary>创建执行器。</summary>
    public SinglePointCalculationExecutor(
        ICalculationWorkspace workspace,
        IComputeBackend computeBackend,
        ICalculationRepository repository,
        ICalculationJobMonitor jobMonitor,
        ISkillInvoker skillInvoker)
    {
        _workspace = workspace;
        _computeBackend = computeBackend;
        _repository = repository;
        _jobMonitor = jobMonitor;
        _skillInvoker = skillInvoker;
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
            string jobId = "job-" + Guid.NewGuid().ToString("N");
            await _workspace.EnsureJobWorkspaceAsync(jobId, cancellationToken);

            string coordinatePath = Path.Combine(
                _workspace.GetInputDirectory(jobId),
                CalculationWorkspacePaths.JobCoordinatesFileName);
            await File.WriteAllTextAsync(coordinatePath, coordinateText, cancellationToken);

            // 化学参数由服务器端默认方案提供，当前不使用客户端参数。
            CalculationSpec spec = CalculationDefaults.CreateDefaultSinglePoint();

            string inputFileName = jobId + ".gjf";
            string inputPath = Path.Combine(_workspace.GetInputDirectory(jobId), inputFileName);
            string runInputPath = Path.Combine(_workspace.GetRunDirectory(jobId), inputFileName);
            string outputPath = _workspace.GetJobOutputPath(jobId);

            CalculationJob job = new CalculationJob();
            job.JobId = jobId;
            job.State = CalculationJobState.Created;
            job.WorkspaceDirectory = _workspace.GetJobDirectory(jobId);
            job.RunDirectory = _workspace.GetRunDirectory(jobId);
            job.OutputFilePath = outputPath;

            CalculationInputGenerationRequest inputRequest =
                new CalculationInputGenerationRequest();
            inputRequest.Job = job;
            inputRequest.Spec = spec;
            inputRequest.CoordinateText = coordinateText;
            inputRequest.InputFilePath = inputPath;
            inputRequest.RunInputFilePath = runInputPath;
            inputRequest.OutputFilePath = outputPath;

            CalculationInputGenerationResult inputResult =
                await _skillInvoker.InvokeAsync<
                    CalculationInputGenerationRequest,
                    CalculationInputGenerationResult>(
                        CalculationSkillIds.GaussianInputGeneration,
                        inputRequest,
                        cancellationToken);

            if (!inputResult.Succeeded)
            {
                result.Succeeded = false;
                result.Error = inputResult.Error;
                result.Diagnostics = new List<string>(inputResult.Diagnostics);
                return result;
            }

            job = inputResult.Job;
            CalculationExecutionContext context = inputResult.ExecutionContext;

            await _computeBackend.SubmitAsync(job, context, CancellationToken.None);

            job.State = CalculationJobState.Running;
            job.StartedAt = DateTimeOffset.UtcNow;

            await _repository.SaveJobAsync(job, CancellationToken.None);
            _jobMonitor.Start(job);

            result.Succeeded = true;
            result.JobId = jobId;
            result.Status = CalculationJobState.Running.ToString();
            result.InputFilePath = inputPath;
            result.OutputFilePath = outputPath;
            result.Message =
                spec.Program +
                " 输入文件已复制到运行目录，计算和输出解析已在后台启动。";

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
