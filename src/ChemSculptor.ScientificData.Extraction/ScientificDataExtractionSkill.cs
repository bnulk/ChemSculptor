using System.Text.Json;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.ScientificData.Extraction.Abstractions;
using ChemSculptor.ScientificData.Extraction.Models;
using ChemSculptor.ScientificData.Models;

namespace ChemSculptor.ScientificData.Extraction;

/// <summary>
/// 单点工作流末端的科学数据提取 Skill。
/// 只负责把上游计算结果翻译为科学成果并写入科学数据仓储。
/// </summary>
public sealed class ScientificDataExtractionSkill : ISkill
{
    private const string RequestKey = "request";
    private const string ValidationKey = "validation";
    private const string ExtractionKey = "extraction";
    private const string StabilityKey = "stability";
    private const string CorrectionPlanKey = "correctionPlan";
    private const string RecoveryExecutionKey = "recoveryExecution";
    private const string RecoveryStabilityKey = "recoveryStability";

    private static readonly JsonSerializerOptions JsonOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly IScientificDataRecorder _recorder;
    private readonly List<string> _capabilities;

    /// <summary>创建科学数据提取 Skill。</summary>
    public ScientificDataExtractionSkill(
        IScientificDataRecorder recorder)
    {
        if (recorder == null)
        {
            throw new ArgumentNullException(nameof(recorder));
        }

        _recorder = recorder;
        _capabilities = new List<string>();
        _capabilities.Add("science.data");
        _capabilities.Add("science.extract-result");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get { return ScientificDataSkillIds.RecordCalculationResult; }
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

    /// <summary>提取并保存科学成果。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        CalculationInputGenerationRequest inputRequest =
            DeserializeRequired<
                CalculationInputGenerationRequest>(
                request,
                RequestKey);
        CalculationWorkflowValidationSkillResult validation =
            DeserializeRequired<
                CalculationWorkflowValidationSkillResult>(
                request,
                ValidationKey);
        CalculationResultExtractionResult extraction =
            DeserializeRequired<
                CalculationResultExtractionResult>(
                request,
                ExtractionKey);
        AnomalyCheckResult stability =
            DeserializeRequired<AnomalyCheckResult>(
                request,
                StabilityKey);
        WavefunctionStabilityCorrectionPlanningResult planning =
            DeserializeRequired<
                WavefunctionStabilityCorrectionPlanningResult>(
                request,
                CorrectionPlanKey);
        RecoveryJobExecutionResult? recoveryExecution =
            DeserializeOptional<RecoveryJobExecutionResult>(
                request,
                RecoveryExecutionKey);
        AnomalyCheckResult? recoveryStability =
            DeserializeOptional<AnomalyCheckResult>(
                request,
                RecoveryStabilityKey);

        ScientificResultExtractionRequest extractionRequest =
            new ScientificResultExtractionRequest();
        extractionRequest.RootWorkflowId = request.WorkflowId;
        extractionRequest.CoordinateText =
            inputRequest.CoordinateText;
        extractionRequest.OriginalJob = validation.Job;
        extractionRequest.OriginalResult = extraction.Result;
        extractionRequest.OriginalValidationReport =
            validation.Report;
        extractionRequest.OriginalStabilityCheck = stability;
        extractionRequest.CorrectionPlanning = planning;
        extractionRequest.RecoveryExecution = recoveryExecution;
        extractionRequest.RecoveryStabilityCheck =
            recoveryStability;

        ScientificResultExtractionResult extractionResult =
            await _recorder.RecordAsync(
                extractionRequest,
                cancellationToken);

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = extractionResult.Succeeded;
        taskResult.Output = JsonSerializer.Serialize(
            extractionResult,
            JsonOptions);

        if (!extractionResult.Succeeded)
        {
            taskResult.Diagnostics = extractionResult.Error;
        }

        return taskResult;
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static T DeserializeRequired<T>(
        TaskRequest taskRequest,
        string key)
    {
        string? json;

        if (!taskRequest.Inputs.TryGetValue(key, out json)
            || string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException(
                "科学数据提取节点缺少 " + key + " 输入。");
        }

        T? value = JsonSerializer.Deserialize<T>(
            json,
            JsonOptions);

        if (value == null)
        {
            throw new InvalidOperationException(
                "无法反序列化科学数据输入：" + key);
        }

        return value;
    }

    private static T? DeserializeOptional<T>(
        TaskRequest taskRequest,
        string key)
    {
        string? json;

        if (!taskRequest.Inputs.TryGetValue(key, out json)
            || string.IsNullOrWhiteSpace(json))
        {
            return default(T);
        }

        return JsonSerializer.Deserialize<T>(
            json,
            JsonOptions);
    }
}
