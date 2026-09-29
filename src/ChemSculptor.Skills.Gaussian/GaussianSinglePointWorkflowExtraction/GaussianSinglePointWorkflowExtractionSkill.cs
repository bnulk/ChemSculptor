using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.Skills.Common;

namespace ChemSculptor.Skills.Gaussian.GaussianSinglePointWorkflowExtraction;

/// <summary>工作流中的 Gaussian 单点结果提取节点。</summary>
public sealed class GaussianSinglePointWorkflowExtractionSkill : ISkill
{
    private readonly IQuantumProgramAdapter _programAdapter;
    private readonly ICalculationRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建工作流结果提取技能。</summary>
    public GaussianSinglePointWorkflowExtractionSkill(
        IQuantumProgramAdapter programAdapter,
        ICalculationRepository repository)
    {
        if (programAdapter == null)
        {
            throw new ArgumentNullException(nameof(programAdapter));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _programAdapter = programAdapter;
        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add("calculation.result-extraction");
        _capabilities.Add("workflow.calculation");
        _capabilities.Add("gaussian.result");
    }

    /// <summary>技能标识。</summary>
    public string Name
    {
        get { return CalculationSkillIds.GaussianSinglePointWorkflowExtraction; }
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

        CalculationJob job = submission.Job;
        CalculationResult result =
            await _programAdapter.ParseOutputAsync(
                job.OutputFilePath,
                cancellationToken);

        result.JobId = job.JobId;
        result.Program = job.Spec.Program;
        result.Method = job.Spec.Method;
        result.Basis = job.Spec.Basis;
        result.Charge = job.Spec.Charge;
        result.Multiplicity = job.Spec.Multiplicity;
        result.OutputFilePath = job.OutputFilePath;

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

    /// <summary>检查技能可用性。</summary>
    public Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
