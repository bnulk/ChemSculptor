using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.CalculationWorkflow;

/// <summary>工作流中的通用处理方案节点。</summary>
public sealed class CalculationWorkflowProcessingPlanSkill : ISkill
{
    private readonly ICalculationRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建处理方案技能。</summary>
    public CalculationWorkflowProcessingPlanSkill(
        ICalculationRepository repository)
    {
        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add("calculation.processing-plan");
        _capabilities.Add("workflow.calculation");
    }

    /// <summary>技能标识。</summary>
    public string Name
    {
        get { return CalculationSkillIds.CalculationWorkflowProcessingPlan; }
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

    /// <summary>生成并保存通用处理方案。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? validationJson;
        string? extractionJson;

        if (!request.Inputs.TryGetValue("validation", out validationJson))
        {
            throw new InvalidOperationException("处理方案节点缺少 validation 输入。");
        }

        if (!request.Inputs.TryGetValue("extraction", out extractionJson))
        {
            throw new InvalidOperationException("处理方案节点缺少 extraction 输入。");
        }

        if (string.IsNullOrWhiteSpace(validationJson)
            || string.IsNullOrWhiteSpace(extractionJson))
        {
            throw new InvalidOperationException("处理方案节点输入不能为空。");
        }

        CalculationWorkflowValidationSkillResult validationResult =
            SkillJson.Deserialize<CalculationWorkflowValidationSkillResult>(
                validationJson);
        CalculationResultExtractionResult extractionResult =
            SkillJson.Deserialize<CalculationResultExtractionResult>(
                extractionJson);

        CalculationProcessingPlan plan =
            CalculationProcessingPlanFactory.Create(
                validationResult.Job,
                extractionResult.Result);

        await _repository.SaveProcessingPlanAsync(plan, cancellationToken);

        CalculationWorkflowProcessingPlanSkillResult result =
            new CalculationWorkflowProcessingPlanSkillResult();
        result.Plan = plan;

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
