using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.Skills.Common.CalculationResultValidation;

namespace ChemSculptor.Skills.Common.CalculationWorkflow;

/// <summary>工作流中的结果验证节点。</summary>
public sealed class CalculationWorkflowValidationSkill : ISkill
{
    private readonly CalculationValidationService _validationService;
    private readonly ICalculationRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建工作流验证技能。</summary>
    public CalculationWorkflowValidationSkill(
        CalculationValidationService validationService,
        ICalculationRepository repository)
    {
        if (validationService == null)
        {
            throw new ArgumentNullException(nameof(validationService));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _validationService = validationService;
        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add("calculation.validation");
        _capabilities.Add("workflow.calculation");
    }

    /// <summary>技能标识。</summary>
    public string Name
    {
        get { return CalculationSkillIds.CalculationWorkflowValidation; }
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

    /// <summary>验证提取结果并更新作业状态。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? waitJson;
        string? extractionJson;

        if (!request.Inputs.TryGetValue("wait", out waitJson))
        {
            throw new InvalidOperationException("验证节点缺少 wait 输入。");
        }

        if (!request.Inputs.TryGetValue("extraction", out extractionJson))
        {
            throw new InvalidOperationException("验证节点缺少 extraction 输入。");
        }

        if (string.IsNullOrWhiteSpace(waitJson)
            || string.IsNullOrWhiteSpace(extractionJson))
        {
            throw new InvalidOperationException("验证节点输入不能为空。");
        }

        CalculationWaitSkillResult waitResult =
            SkillJson.Deserialize<CalculationWaitSkillResult>(waitJson);
        CalculationResultExtractionResult extractionResult =
            SkillJson.Deserialize<CalculationResultExtractionResult>(
                extractionJson);

        CalculationResultValidationRequest validationRequest =
            new CalculationResultValidationRequest();
        validationRequest.Job = waitResult.Job;
        validationRequest.Result = extractionResult.Result;

        CalculationResultValidationResult validationResult =
            _validationService.Validate(validationRequest);

        if (waitResult.State == CalculationJobState.Completed
            && validationResult.Passed)
        {
            waitResult.Job.State = CalculationJobState.Validated;
        }
        else
        {
            waitResult.Job.State = CalculationJobState.Failed;
        }

        await _repository.SaveValidationAsync(
            validationResult.Report,
            waitResult.Job.JobId,
            cancellationToken);
        await _repository.SaveJobAsync(
            waitResult.Job,
            cancellationToken);

        CalculationWorkflowValidationSkillResult result =
            new CalculationWorkflowValidationSkillResult();
        result.Passed = validationResult.Passed;
        result.Job = waitResult.Job;
        result.Report = validationResult.Report;

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
