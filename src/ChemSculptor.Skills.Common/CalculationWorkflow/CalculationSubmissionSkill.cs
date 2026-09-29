using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.CalculationWorkflow;

/// <summary>把已经生成的输入提交给计算后端。</summary>
public sealed class CalculationSubmissionSkill : ISkill
{
    private readonly IComputeBackend _computeBackend;
    private readonly ICalculationRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建计算提交技能。</summary>
    public CalculationSubmissionSkill(
        IComputeBackend computeBackend,
        ICalculationRepository repository)
    {
        if (computeBackend == null)
        {
            throw new ArgumentNullException(nameof(computeBackend));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _computeBackend = computeBackend;
        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add("calculation.submit");
        _capabilities.Add("workflow.calculation");
    }

    /// <summary>技能标识。</summary>
    public string Name
    {
        get { return CalculationSkillIds.CalculationSubmission; }
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

    /// <summary>提交计算。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? inputJson;
        if (!request.Inputs.TryGetValue("inputResult", out inputJson))
        {
            throw new InvalidOperationException("提交技能缺少 inputResult 输入。");
        }

        if (string.IsNullOrWhiteSpace(inputJson))
        {
            throw new InvalidOperationException("提交技能的 inputResult 不能为空。");
        }

        CalculationInputGenerationResult inputResult =
            SkillJson.Deserialize<CalculationInputGenerationResult>(inputJson);

        CalculationJob job = inputResult.Job;
        job.State = CalculationJobState.Running;
        job.StartedAt = DateTimeOffset.UtcNow;

        await _computeBackend.SubmitAsync(
            job,
            inputResult.ExecutionContext,
            cancellationToken);
        await _repository.SaveJobAsync(job, cancellationToken);

        CalculationSubmissionSkillResult result =
            new CalculationSubmissionSkillResult();
        result.Succeeded = true;
        result.Job = job;
        result.ExecutionContext = inputResult.ExecutionContext;

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(result);
        return taskResult;
    }

    /// <summary>检查技能可用性。</summary>
    public Task<bool> HealthAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
