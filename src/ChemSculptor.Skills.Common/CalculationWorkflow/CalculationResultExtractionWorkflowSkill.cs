using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.CalculationWorkflow;

/// <summary>
/// 通用计算结果提取技能。
/// 具体输出格式由程序适配器负责。
/// </summary>
public sealed class CalculationResultExtractionWorkflowSkill : ISkill
{
    private readonly IQuantumProgramAdapterRegistry _adapterRegistry;
    private readonly ICalculationRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建通用结果提取技能。</summary>
    public CalculationResultExtractionWorkflowSkill(
        IQuantumProgramAdapterRegistry adapterRegistry,
        ICalculationRepository repository)
    {
        if (adapterRegistry == null)
        {
            throw new ArgumentNullException(nameof(adapterRegistry));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _adapterRegistry = adapterRegistry;
        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add("calculation.extract-result");
        _capabilities.Add("workflow.calculation");
    }

    /// <summary>技能标识。</summary>
    public string Name
    {
        get { return CalculationSkillIds.CalculationResultExtraction; }
    }

    /// <summary>技能版本。</summary>
    public string Version
    {
        get { return "1.0.0"; }
    }

    /// <summary>技能能力。</summary>
    public IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>提取结果并保存。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? submissionJson;
        if (!request.Inputs.TryGetValue("submission", out submissionJson))
        {
            throw new InvalidOperationException("结果提取节点缺少 submission 输入。");
        }

        if (string.IsNullOrWhiteSpace(submissionJson))
        {
            throw new InvalidOperationException("结果提取节点的 submission 不能为空。");
        }

        CalculationSubmissionSkillResult submission =
            SkillJson.Deserialize<CalculationSubmissionSkillResult>(
                submissionJson);

        IQuantumProgramAdapter? adapter =
            _adapterRegistry.Resolve(submission.Job.Spec);

        if (adapter == null)
        {
            throw new InvalidOperationException(
                "没有可处理 " +
                submission.Job.Spec.Program +
                " 的计算程序适配器。");
        }

        if (submission.Job.Spec.TaskType
            == CalculationTaskType.Optimization)
        {
            return await ExtractOptimizationAsync(
                request,
                submission,
                adapter,
                cancellationToken);
        }

        CalculationJob job = submission.Job;
        CalculationResult result =
            await adapter.ParseOutputAsync(
                job.OutputFilePath,
                cancellationToken);

        result.JobId = job.JobId;
        result.Program = job.Spec.Program;
        result.Method = job.Spec.Method;
        result.Basis = job.Spec.Basis;
        result.Charge = job.Spec.Charge;
        result.Multiplicity = job.Spec.Multiplicity;
        result.OutputFilePath = job.OutputFilePath;

        CalculationArtifactDiscoveryContext artifactContext =
            new CalculationArtifactDiscoveryContext();
        artifactContext.Job = job;
        artifactContext.Result = result;
        IReadOnlyList<CalculationArtifactPattern> artifactPatterns =
            adapter.GetArtifactPatterns(artifactContext);
        result.Artifacts = CalculationArtifactCollector.Collect(
            job.RunDirectory,
            artifactPatterns);

        await _repository.SaveResultAsync(result, cancellationToken);

        CalculationResultExtractionResult extractionResult =
            new CalculationResultExtractionResult();
        extractionResult.Succeeded = true;
        extractionResult.Result = result;

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(extractionResult);
        return taskResult;
    }

    private static async Task<TaskResult>
        ExtractOptimizationAsync(
            TaskRequest request,
            CalculationSubmissionSkillResult submission,
            IQuantumProgramAdapter adapter,
            CancellationToken cancellationToken)
    {
        if (!(adapter is IOptimizationOutputParser optimizationParser))
        {
            throw new NotSupportedException(
                "当前程序适配器不支持几何优化结果解析。");
        }

        CalculationJob job = submission.Job;
        OptimizationResult result =
            await optimizationParser.ParseOptimizationOutputAsync(
                job.OutputFilePath,
                cancellationToken);
        result.JobId = job.JobId;
        result.Program = job.Spec.Program;
        result.Method = job.Spec.Method;
        result.Basis = job.Spec.Basis;
        result.Charge = job.Spec.Charge;
        result.Multiplicity = job.Spec.Multiplicity;
        result.OutputFilePath = job.OutputFilePath;

        CalculationResult artifactResult =
            new CalculationResult();
        artifactResult.JobId = job.JobId;
        artifactResult.Program = job.Spec.Program;
        artifactResult.Method = job.Spec.Method;
        artifactResult.Basis = job.Spec.Basis;
        artifactResult.Charge = job.Spec.Charge;
        artifactResult.Multiplicity = job.Spec.Multiplicity;
        artifactResult.OutputFilePath = job.OutputFilePath;
        artifactResult.NormalTermination =
            result.NormalTermination;
        artifactResult.Energy = result.FinalEnergy;

        CalculationArtifactDiscoveryContext artifactContext =
            new CalculationArtifactDiscoveryContext();
        artifactContext.Job = job;
        artifactContext.Result = artifactResult;
        IReadOnlyList<CalculationArtifactPattern> artifactPatterns =
            adapter.GetArtifactPatterns(artifactContext);
        result.Artifacts = CalculationArtifactCollector.Collect(
            job.RunDirectory,
            artifactPatterns);

        CalculationResultExtractionResult extractionResult =
            new CalculationResultExtractionResult();
        extractionResult.Succeeded = true;
        extractionResult.OptimizationResult = result;

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(extractionResult);
        return taskResult;
    }

    /// <summary>检查技能可用性。</summary>
    public Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
