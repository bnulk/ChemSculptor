using ChemSculptor.Anomaly.Abstractions;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Registry;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Compute;
using ChemSculptor.Domain;

namespace ChemSculptor.Skills.Common.AnomalyWorkflow;

/// <summary>
/// 通用波函数稳定性检查 Skill。
/// 根据计算程序选择具体实现，并统一返回 AnomalyCheckResult。
/// </summary>
public sealed class WavefunctionStabilityCheckSkill
    : ISkill
{
    private const string RequestKey = "request";
    private const string ValidationKey = "validation";
    private const string RecoveryExecutionKey = "recoveryExecution";

    private readonly IAnomalyProviderRegistry _registry;
    private readonly IAnomalyRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建通用稳定性检查 Skill。</summary>
    public WavefunctionStabilityCheckSkill(
        IAnomalyProviderRegistry registry,
        IAnomalyRepository repository)
    {
        if (registry == null)
        {
            throw new ArgumentNullException(nameof(registry));
        }

        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _registry = registry;
        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add("anomaly.calculation-check");
        _capabilities.Add("anomaly.scientific-check");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get { return AnomalySkillIds.CheckWavefunctionStability; }
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

    /// <summary>执行稳定性检查并保存异常记录。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        AnomalyCheckRequest? checkRequest = null;
        AnomalyCheckResult checkResult;

        if (TryCreateSkippedRecoveryResult(
            request,
            out AnomalyCheckResult? skippedResult))
        {
            checkResult = skippedResult!;
        }
        else
        {
            checkRequest = DeserializeCheckRequest(request);
            checkResult = await ExecuteCheckAsync(
                checkRequest,
                cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(checkResult.JobId)
            && checkRequest != null
            && checkRequest.Context.Job != null)
        {
            checkResult.JobId = checkRequest.Context.Job.JobId;
        }

        if (checkRequest != null
            && checkRequest.Context.Job != null)
        {
            AnomalyRecord record =
                CreateAnomalyRecord(
                    checkRequest.Context,
                    checkResult);
            checkResult.AnomalyRecordId = record.Id;
            await _repository.SaveAsync(
                record,
                cancellationToken);
        }

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = true;
        taskResult.Output = SkillJson.Serialize(checkResult);
        return taskResult;
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private async Task<AnomalyCheckResult> ExecuteCheckAsync(
        AnomalyCheckRequest request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.CheckCode)
            && !string.Equals(
                request.CheckCode,
                CommonAnomalyCheckCodes.WavefunctionStability,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "通用稳定性检查 Skill 收到了不支持的检查代码：" +
                request.CheckCode);
        }

        List<IAnomalyCheck> candidates = new List<IAnomalyCheck>();
        IReadOnlyList<IAnomalyCheck> checks = _registry.ListChecks();

        for (int index = 0; index < checks.Count; index++)
        {
            IAnomalyCheck check = checks[index];

            if (!string.Equals(
                check.Descriptor.Code,
                CommonAnomalyCheckCodes.WavefunctionStability,
                StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (check.CanCheck(request.Context))
            {
                candidates.Add(check);
            }
        }

        if (candidates.Count == 0)
        {
            return CreateUnavailableResult();
        }

        candidates.Sort(CompareChecks);
        return await candidates[0].CheckAsync(
            request.Context,
            cancellationToken);
    }

    private static AnomalyCheckRequest DeserializeCheckRequest(
        TaskRequest taskRequest)
    {
        string? requestJson;

        if (taskRequest.Inputs.TryGetValue(RequestKey, out requestJson)
            && !string.IsNullOrWhiteSpace(requestJson))
        {
            return SkillJson.Deserialize<AnomalyCheckRequest>(
                requestJson);
        }

        string? validationJson;

        if (taskRequest.Inputs.TryGetValue(
            ValidationKey,
            out validationJson)
            && !string.IsNullOrWhiteSpace(validationJson))
        {
            CalculationWorkflowValidationSkillResult validation =
                SkillJson.Deserialize<
                    CalculationWorkflowValidationSkillResult>(
                    validationJson);
            AnomalyCheckRequest request =
                new AnomalyCheckRequest();
            request.CheckCode =
                CommonAnomalyCheckCodes.WavefunctionStability;
            request.Context = new AnomalyContext();
            request.Context.Job = validation.Job;
            request.Context.Validation = validation.Report;
            return request;
        }

        string? recoveryExecutionJson;

        if (taskRequest.Inputs.TryGetValue(
            RecoveryExecutionKey,
            out recoveryExecutionJson)
            && !string.IsNullOrWhiteSpace(recoveryExecutionJson))
        {
            RecoveryJobExecutionResult recoveryExecution =
                SkillJson.Deserialize<RecoveryJobExecutionResult>(
                    recoveryExecutionJson);

            AnomalyCheckRequest request =
                new AnomalyCheckRequest();
            request.CheckCode =
                CommonAnomalyCheckCodes.WavefunctionStability;
            request.Context = new AnomalyContext();
            request.Context.Job =
                recoveryExecution.RecoveryJob;
            request.Context.Result =
                recoveryExecution.Result;
            return request;
        }

        throw new InvalidOperationException(
            "稳定性检查 Skill 缺少 request、validation 或 recoveryExecution 输入。");
    }

    /// <summary>没有派生恢复作业时直接返回跳过结果。</summary>
    private static bool TryCreateSkippedRecoveryResult(
        TaskRequest taskRequest,
        out AnomalyCheckResult? result)
    {
        string? recoveryExecutionJson;

        if (!taskRequest.Inputs.TryGetValue(
            RecoveryExecutionKey,
            out recoveryExecutionJson)
            || string.IsNullOrWhiteSpace(recoveryExecutionJson))
        {
            result = null;
            return false;
        }

        RecoveryJobExecutionResult recoveryExecution =
            SkillJson.Deserialize<RecoveryJobExecutionResult>(
                recoveryExecutionJson);

        if (recoveryExecution.RecoveryJob != null)
        {
            result = null;
            return false;
        }

        result = CreateSkippedResult(
            "没有派生恢复作业，跳过波函数稳定性复检。");
        return true;
    }

    private static AnomalyRecord CreateAnomalyRecord(
        AnomalyContext context,
        AnomalyCheckResult checkResult)
    {
        AnomalyRecord record = new AnomalyRecord();
        record.Id = "anomaly-" + Guid.NewGuid().ToString("N");
        record.JobId = context.Job!.JobId;
        record.WorkflowId = context.Job.WorkflowId;
        record.Status = MapStatus(checkResult.Status);
        record.Checks.Add(checkResult);

        if (checkResult.Findings.Count > 0)
        {
            AnomalyAssessment assessment = new AnomalyAssessment();
            assessment.JobId = record.JobId;
            assessment.WorkflowId = record.WorkflowId;
            assessment.Findings = new List<AnomalyFinding>(
                checkResult.Findings);
            record.Assessment = assessment;
        }

        return record;
    }

    private static AnomalyRecordStatus MapStatus(
        AnomalyCheckStatus status)
    {
        if (status == AnomalyCheckStatus.Passed)
        {
            return AnomalyRecordStatus.Passed;
        }

        if (status == AnomalyCheckStatus.Finding
            || status == AnomalyCheckStatus.Inconclusive)
        {
            return AnomalyRecordStatus.Open;
        }

        if (status == AnomalyCheckStatus.Skipped)
        {
            return AnomalyRecordStatus.Closed;
        }

        if (status == AnomalyCheckStatus.ExecutionFailed
            || status == AnomalyCheckStatus.Canceled)
        {
            return AnomalyRecordStatus.Failed;
        }

        return AnomalyRecordStatus.Open;
    }

    private static int CompareChecks(
        IAnomalyCheck left,
        IAnomalyCheck right)
    {
        return string.Compare(
            left.Descriptor.ImplementationId,
            right.Descriptor.ImplementationId,
            StringComparison.OrdinalIgnoreCase);
    }

    private static AnomalyCheckResult CreateUnavailableResult()
    {
        return CreateSkippedResult(
            "没有适用于当前任务和计算程序的波函数稳定性检查实现。");
    }

    private static AnomalyCheckResult CreateSkippedResult(
        string reason)
    {
        AnomalyCheckResult result = new AnomalyCheckResult();
        result.Code = CommonAnomalyCheckCodes.WavefunctionStability;
        result.DisplayName = "波函数稳定性检查";
        result.Category = AnomalyCategory.Scientific;
        result.Mechanism = AnomalyCheckMechanism.AuxiliaryCalculation;
        result.IsRequired = true;
        result.Status = AnomalyCheckStatus.Skipped;
        result.SkippedReason = reason;
        result.Summary = result.SkippedReason;
        result.StartedAt = DateTimeOffset.UtcNow;
        result.CompletedAt = DateTimeOffset.UtcNow;
        return result;
    }
}
