using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.CalculationWorkflow;

/// <summary>等待计算作业结束。</summary>
public sealed class CalculationWaitSkill : ISkill
{
    private const int PollingIntervalMilliseconds = 500;

    private readonly IComputeBackend _computeBackend;
    private readonly ICalculationRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建等待技能。</summary>
    public CalculationWaitSkill(
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
        _capabilities.Add("calculation.wait");
        _capabilities.Add("workflow.calculation");
    }

    /// <summary>技能标识。</summary>
    public string Name
    {
        get { return CalculationSkillIds.CalculationWait; }
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

    /// <summary>等待作业进入终止状态。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? submissionJson;
        if (!request.Inputs.TryGetValue("submission", out submissionJson))
        {
            throw new InvalidOperationException("等待技能缺少 submission 输入。");
        }

        if (string.IsNullOrWhiteSpace(submissionJson))
        {
            throw new InvalidOperationException("等待技能的 submission 不能为空。");
        }

        CalculationSubmissionSkillResult submission =
            SkillJson.Deserialize<CalculationSubmissionSkillResult>(
                submissionJson);

        CalculationJobState state = CalculationJobState.Running;

        while (!IsTerminalState(state))
        {
            state = await _computeBackend.GetStatusAsync(
                submission.Job,
                cancellationToken);

            if (!IsTerminalState(state))
            {
                await Task.Delay(
                    PollingIntervalMilliseconds,
                    cancellationToken);
            }
        }

        submission.Job.State = state;
        submission.Job.CompletedAt = DateTimeOffset.UtcNow;
        await _repository.SaveJobAsync(submission.Job, cancellationToken);

        CalculationWaitSkillResult result = new CalculationWaitSkillResult();
        result.State = state;
        result.Job = submission.Job;

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

    private static bool IsTerminalState(CalculationJobState state)
    {
        return state == CalculationJobState.Completed
            || state == CalculationJobState.Failed
            || state == CalculationJobState.Canceled;
    }
}
