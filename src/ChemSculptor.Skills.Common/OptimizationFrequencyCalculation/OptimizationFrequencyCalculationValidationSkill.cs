using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.Skills.Common.CalculationResultValidation;

namespace ChemSculptor.Skills.Common.OptimizationFrequencyCalculation;

/// <summary>验证同一次作业中的优化和振动分析结果。</summary>
public sealed class OptimizationFrequencyCalculationValidationSkill
    : ISkill
{
    private readonly CalculationValidationService _validationService;
    private readonly ICalculationRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建联合验证 Skill。</summary>
    public OptimizationFrequencyCalculationValidationSkill(
        CalculationValidationService validationService,
        ICalculationRepository repository)
    {
        if (validationService == null)
        {
            throw new ArgumentNullException(
                nameof(validationService));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _validationService = validationService;
        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add(
            CalculationSkillIds
                .CalculationOptimizationFrequencyValidation);
        _capabilities.Add("calculation.validation");
        _capabilities.Add("workflow.calculation");
    }

    /// <summary>技能标识。</summary>
    public string Name
    {
        get
        {
            return CalculationSkillIds
                .CalculationOptimizationFrequencyValidation;
        }
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

    /// <summary>验证联合结果。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        string? waitJson;
        string? extractionJson;

        if (!request.Inputs.TryGetValue("wait", out waitJson)
            || string.IsNullOrWhiteSpace(waitJson))
        {
            throw new InvalidOperationException(
                "联合优化频率验证节点缺少 wait 输入。");
        }

        if (!request.Inputs.TryGetValue(
                "extraction",
                out extractionJson)
            || string.IsNullOrWhiteSpace(extractionJson))
        {
            throw new InvalidOperationException(
                "联合优化频率验证节点缺少 extraction 输入。");
        }

        CalculationWaitSkillResult wait =
            SkillJson.Deserialize<CalculationWaitSkillResult>(
                waitJson);
        CalculationResultExtractionResult extraction =
            SkillJson.Deserialize<
                CalculationResultExtractionResult>(
                    extractionJson);

        if (extraction.OptimizationResult == null)
        {
            throw new InvalidOperationException(
                "联合结果中没有 OptimizationResult。");
        }

        if (extraction.FrequencyResult == null)
        {
            throw new InvalidOperationException(
                "联合结果中没有 FrequencyAnalysisResult。");
        }

        CalculationResultValidationRequest validationRequest =
            new CalculationResultValidationRequest();
        validationRequest.Job = wait.Job;
        validationRequest.Result = extraction.Result;
        validationRequest.OptimizationResult =
            extraction.OptimizationResult;
        validationRequest.FrequencyResult =
            extraction.FrequencyResult;

        CalculationResultValidationResult validation =
            _validationService.Validate(validationRequest);
        wait.Job.State =
            wait.State == CalculationJobState.Completed
            && validation.Passed
                ? CalculationJobState.Validated
                : CalculationJobState.Failed;

        await _repository.SaveValidationAsync(
            validation.Report,
            wait.Job.JobId,
            cancellationToken);
        await _repository.SaveJobAsync(
            wait.Job,
            cancellationToken);

        OptimizationFrequencyValidationSkillResult result =
            new OptimizationFrequencyValidationSkillResult();
        result.Passed = validation.Passed;
        result.Job = wait.Job;
        result.OptimizationResult =
            extraction.OptimizationResult;
        result.FrequencyResult = extraction.FrequencyResult;
        result.Report = validation.Report;

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(result);
        return taskResult;
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }
}
