using System.Text.Json;
using ChemSculptor.Compute;
using ChemSculptor.Core;
using ChemSculptor.Domain;

namespace ChemSculptor.Agent;

/// <summary>NTO Workflow 的编排服务。</summary>
public sealed class NtoWorkflowService
    : INtoWorkflowService
{
    private readonly ITddftCalculationService _tddftService;
    private readonly ISkillRegistry _skills;

    /// <summary>创建 NTO Workflow 服务。</summary>
    public NtoWorkflowService(
        ITddftCalculationService tddftService,
        ISkillRegistry skills)
    {
        if (tddftService == null)
        {
            throw new ArgumentNullException(
                nameof(tddftService));
        }

        if (skills == null)
        {
            throw new ArgumentNullException(nameof(skills));
        }

        _tddftService = tddftService;
        _skills = skills;
    }

    /// <summary>执行 NTO Workflow。</summary>
    public async Task<NtoWorkflowResult> SubmitAsync(
        NtoRequest request,
        CancellationToken cancellationToken = default)
    {
        NtoWorkflowResult result = new NtoWorkflowResult();

        if (request == null)
        {
            result.Status = NtoWorkflowStatus.Failed;
            result.Message = "NTO 请求不能为空。";
            return result;
        }

        if (string.IsNullOrWhiteSpace(
            request.SourceTddftJobId))
        {
            result.Status = NtoWorkflowStatus.Failed;
            result.Message = "NTO 计算必须指定源 TDDFT 作业标识。";
            return result;
        }

        CalculationJob? sourceJob =
            await _tddftService.GetJobAsync(
                request.SourceTddftJobId,
                cancellationToken);
        TddftCalculationResult? sourceResult =
            await _tddftService.GetResultAsync(
                request.SourceTddftJobId,
                cancellationToken);

        if (sourceJob == null || sourceResult == null)
        {
            result.Status = NtoWorkflowStatus.Failed;
            result.SourceTddftJobId =
                request.SourceTddftJobId;
            result.Message =
                "找不到可用的源 TDDFT 作业或结果。";
            return result;
        }

        if (!sourceResult.NormalTermination)
        {
            result.Status = NtoWorkflowStatus.Failed;
            result.SourceTddftJobId =
                request.SourceTddftJobId;
            result.Message =
                "源 TDDFT 作业没有正常结束，不能计算 NTO。";
            return result;
        }

        result.SourceTddftJobId = sourceJob.JobId;
        result.SelectionMode = request.SelectionMode;
        NtoStateSelectionResult selection =
            NtoStateSelector.Select(
                sourceResult,
                request.SelectionMode,
                request.WavelengthMinNm,
                request.WavelengthMaxNm);

        if (selection.Outcome
            == NtoSelectionOutcome
                .NoPositiveOscillatorStrength)
        {
            result.Succeeded = true;
            result.Status =
                NtoWorkflowStatus
                    .NoPositiveOscillatorStrength;
            result.Message = selection.Message;
            return result;
        }

        string sourceInputPath =
            sourceJob.InputFilePath;
        string sourceCheckpointPath =
            FindCheckpointPath(
                sourceJob,
                sourceResult);

        if (string.IsNullOrWhiteSpace(sourceInputPath)
            || !File.Exists(sourceInputPath)
            || string.IsNullOrWhiteSpace(
                sourceCheckpointPath)
            || !File.Exists(sourceCheckpointPath))
        {
            result.Status = NtoWorkflowStatus.Failed;
            result.SelectedStates = selection.States;
            result.Message =
                "源 TDDFT 输入或检查点文件缺失，不能计算 NTO。";
            return result;
        }

        ISkill? ntoSkill =
            _skills.Resolve(
                CalculationSkillIds.CalculationNtoTransition);

        if (ntoSkill == null)
        {
            result.Status = NtoWorkflowStatus.Failed;
            result.SelectedStates = selection.States;
            result.Message =
                "NTO Transition Skill 尚未注册。";
            return result;
        }

        result.SelectedStates = selection.States;

        for (int index = 0;
            index < selection.States.Count;
            index++)
        {
            NtoSelectedState state =
                selection.States[index];
            state.StateId = BuildStateId(
                sourceJob.JobId,
                state.StateIndex);

            NtoTransitionRequest transitionRequest =
                new NtoTransitionRequest();
            transitionRequest.SourceJob = sourceJob;
            transitionRequest.SourceSpec = sourceJob.Spec;
            transitionRequest.SourceInputPath =
                sourceInputPath;
            transitionRequest.SourceCheckpointPath =
                sourceCheckpointPath;
            transitionRequest.SelectedState = state;
            transitionRequest.Goal = request.Goal;

            TaskRequest taskRequest = new TaskRequest();
            taskRequest.WorkflowId =
                "nto-" + sourceJob.JobId;
            taskRequest.NodeId =
                "nto-transition-" +
                state.StateIndex.ToString();
            taskRequest.SkillId = ntoSkill.Name;
            taskRequest.Inputs["request"] =
                Serialize(transitionRequest);

            TaskResult taskResult =
                await ntoSkill.ExecuteAsync(
                    taskRequest,
                    cancellationToken);
            NtoTransitionResult transition =
                DeserializeTransition(taskResult);
            result.TransitionResults.Add(transition);
        }

        int successCount = 0;

        for (int index = 0;
            index < result.TransitionResults.Count;
            index++)
        {
            if (result.TransitionResults[index].Succeeded)
            {
                successCount++;
            }
        }

        result.Succeeded =
            successCount == result.TransitionResults.Count;
        result.Status = result.Succeeded
            ? NtoWorkflowStatus.Completed
            : NtoWorkflowStatus.Partial;
        result.Message = result.Succeeded
            ? "NTO 工作流已完成，共计算 " +
                result.TransitionResults.Count.ToString() +
                " 个激发态。"
            : "NTO 工作流部分完成，成功 " +
                successCount.ToString() +
                " 个，失败 " +
                (result.TransitionResults.Count - successCount)
                    .ToString() +
                " 个。";
        return result;
    }

    private static string FindCheckpointPath(
        CalculationJob sourceJob,
        TddftCalculationResult sourceResult)
    {
        for (int index = 0;
            index < sourceResult.Artifacts.Count;
            index++)
        {
            CalculationArtifactDescriptor artifact =
                sourceResult.Artifacts[index];

            if (artifact.RelativePath.EndsWith(
                ".chk",
                StringComparison.OrdinalIgnoreCase))
            {
                string path = Path.Combine(
                    sourceJob.RunDirectory,
                    artifact.RelativePath);

                if (File.Exists(path))
                {
                    return path;
                }
            }
        }

        if (Directory.Exists(sourceJob.RunDirectory))
        {
            string[] checkpoints =
                Directory.GetFiles(
                    sourceJob.RunDirectory,
                    "*.chk",
                    SearchOption.TopDirectoryOnly);

            if (checkpoints.Length > 0)
            {
                return checkpoints[0];
            }
        }

        return string.Empty;
    }

    private static string BuildStateId(
        string sourceJobId,
        int stateIndex)
    {
        return "tddft:" +
            sourceJobId +
            ":state:" +
            stateIndex.ToString();
    }

    private static NtoTransitionResult
        DeserializeTransition(TaskResult taskResult)
    {
        if (!taskResult.Succeeded
            || string.IsNullOrWhiteSpace(taskResult.Output))
        {
            NtoTransitionResult failed =
                new NtoTransitionResult();
            failed.Error = string.IsNullOrWhiteSpace(
                    taskResult.Diagnostics)
                ? "NTO Transition Skill 执行失败。"
                : taskResult.Diagnostics;
            return failed;
        }

        return JsonSerializer.Deserialize<
            NtoTransitionResult>(
                taskResult.Output,
                CreateJsonOptions())
            ?? new NtoTransitionResult();
    }

    private static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(
            value,
            CreateJsonOptions());
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        return new JsonSerializerOptions(
            JsonSerializerDefaults.Web);
    }
}
