using System.Globalization;
using ChemSculptor.Compute;
using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Agent;

/// <summary>
/// 吸收光谱工作流。
/// 负责按模式编排几何优化和 TDDFT 单点计算。
/// </summary>
public sealed class AbsorptionSpectrumWorkflowService
    : IAbsorptionSpectrumWorkflowService
{
    private readonly IOptimizationCalculationService
        _optimizationService;
    private readonly ITddftCalculationService _tddftService;
    private readonly IAbsorptionSpectrumResultRecorder?
        _resultRecorder;
    private readonly IAbsorptionSpectrumRecoveryFramework
        _recoveryFramework;

    /// <summary>创建吸收光谱工作流服务。</summary>
    public AbsorptionSpectrumWorkflowService(
        IOptimizationCalculationService optimizationService,
        ITddftCalculationService tddftService)
        : this(
            optimizationService,
            tddftService,
            null,
            new NoOpAbsorptionSpectrumRecoveryFramework())
    {
    }

    /// <summary>创建带科学数据归档的吸收光谱工作流服务。</summary>
    public AbsorptionSpectrumWorkflowService(
        IOptimizationCalculationService optimizationService,
        ITddftCalculationService tddftService,
        IAbsorptionSpectrumResultRecorder? resultRecorder)
        : this(
            optimizationService,
            tddftService,
            resultRecorder,
            new NoOpAbsorptionSpectrumRecoveryFramework())
    {
    }

    /// <summary>创建带归档和恢复框架的吸收光谱工作流服务。</summary>
    public AbsorptionSpectrumWorkflowService(
        IOptimizationCalculationService optimizationService,
        ITddftCalculationService tddftService,
        IAbsorptionSpectrumResultRecorder? resultRecorder,
        IAbsorptionSpectrumRecoveryFramework recoveryFramework)
    {
        if (optimizationService == null)
        {
            throw new ArgumentNullException(
                nameof(optimizationService));
        }

        if (tddftService == null)
        {
            throw new ArgumentNullException(
                nameof(tddftService));
        }

        if (recoveryFramework == null)
        {
            throw new ArgumentNullException(
                nameof(recoveryFramework));
        }

        _optimizationService = optimizationService;
        _tddftService = tddftService;
        _resultRecorder = resultRecorder;
        _recoveryFramework = recoveryFramework;
    }

    /// <summary>执行吸收光谱工作流。</summary>
    public async Task<AbsorptionSpectrumSubmissionResult> SubmitAsync(
        AbsorptionSpectrumRequest request,
        CancellationToken cancellationToken = default)
    {
        AbsorptionSpectrumSubmissionResult result =
            new AbsorptionSpectrumSubmissionResult();

        if (request == null)
        {
            result.Error = "吸收光谱请求不能为空。";
            return result;
        }

        if (string.IsNullOrWhiteSpace(request.CoordinateText))
        {
            result.Error = "坐标文本不能为空。";
            return result;
        }

        CalculationSpec tddftSpec = request.TddftSpec
            ?? CalculationDefaults.CreateDefaultTddftSinglePoint();

        if (tddftSpec.TaskType != CalculationTaskType.TdDft)
        {
            result.Error = "吸收光谱工作流要求 TDDFT 方案。";
            return result;
        }

        result.Mode = request.Mode;
        string tddftCoordinateText = request.CoordinateText;

        if (request.Mode != AbsorptionSpectrumMode.TddftOnly)
        {
            CalculationSpec optimizationSpec =
                CreateOptimizationSpec(
                    tddftSpec,
                    request.Mode);
            OptimizationRequest optimizationRequest =
                new OptimizationRequest();
            optimizationRequest.SessionId = request.SessionId;
            optimizationRequest.GeometryId = request.GeometryId;
            optimizationRequest.Goal = request.Goal;
            optimizationRequest.CoordinateText =
                request.CoordinateText;
            optimizationRequest.Charge = optimizationSpec.Charge;
            optimizationRequest.Multiplicity =
                optimizationSpec.Multiplicity;
            optimizationRequest.ResolvedSpec = optimizationSpec;
            optimizationRequest.ResolvedParameters =
                FilterOptimizationParameters(
                    request.ResolvedParameters);

            OptimizationSubmissionResult optimization =
                await _optimizationService.SubmitAsync(
                    optimizationRequest,
                    cancellationToken);
            result.Optimization = optimization;

            if (!optimization.Succeeded
                || optimization.Result == null
                || optimization.Result.FinalGeometry == null
                || optimization.Result.FinalGeometry.Atoms.Count == 0)
            {
                string error = string.IsNullOrWhiteSpace(
                        optimization.Error)
                    ? "几何优化阶段没有返回最终结构。"
                    : optimization.Error;
                await SetFailureAsync(
                    result,
                    request,
                    AbsorptionSpectrumFailureStage.Optimization,
                    AbsorptionSpectrumAnomalyCodes
                        .OptimizationFailed,
                    error,
                    optimization,
                    null,
                    cancellationToken);
                return result;
            }

            tddftCoordinateText = FormatGeometry(
                optimization.Result.FinalGeometry);
        }

        TddftCalculationRequest tddftRequest =
            new TddftCalculationRequest();
        tddftRequest.SessionId = request.SessionId;
        tddftRequest.GeometryId = request.GeometryId;
        tddftRequest.Goal = request.Goal;
        tddftRequest.CoordinateText = tddftCoordinateText;
        tddftRequest.ResolvedSpec = tddftSpec;
        tddftRequest.ResolvedParameters =
            new List<ResolvedCalculationParameter>(
                request.ResolvedParameters);

        TddftSubmissionResult tddft =
            await _tddftService.SubmitAsync(
                tddftRequest,
                cancellationToken);
        result.Tddft = tddft;

        if (!tddft.Succeeded || tddft.Result == null)
        {
            string error = string.IsNullOrWhiteSpace(tddft.Error)
                ? "TDDFT 阶段没有返回结果。"
                : tddft.Error;
            await SetFailureAsync(
                result,
                request,
                AbsorptionSpectrumFailureStage.Tddft,
                AbsorptionSpectrumAnomalyCodes.TddftFailed,
                error,
                result.Optimization,
                tddft,
                cancellationToken);
            return result;
        }

        result.Succeeded = true;
        result.Message = BuildSuccessMessage(
            request.Mode,
            tddft.Result);

        if (_resultRecorder != null)
        {
            AbsorptionSpectrumArchiveRequest archiveRequest =
                new AbsorptionSpectrumArchiveRequest();
            archiveRequest.Request = request;
            archiveRequest.Result = result;
            AbsorptionSpectrumArchiveResult archiveResult =
                await _resultRecorder.RecordAsync(
                    archiveRequest,
                    cancellationToken);

            if (!archiveResult.Succeeded)
            {
                string error = string.IsNullOrWhiteSpace(
                        archiveResult.Error)
                    ? "吸收光谱科学数据归档失败。"
                    : archiveResult.Error;
                await SetFailureAsync(
                    result,
                    request,
                    AbsorptionSpectrumFailureStage.Archive,
                    AbsorptionSpectrumAnomalyCodes.ArchiveFailed,
                    error,
                    result.Optimization,
                    tddft,
                    cancellationToken);
                return result;
            }

            result.ScientificResultId =
                archiveResult.ScientificResultId;
        }

        return result;
    }

    private async Task SetFailureAsync(
        AbsorptionSpectrumSubmissionResult result,
        AbsorptionSpectrumRequest request,
        AbsorptionSpectrumFailureStage stage,
        string failureCode,
        string error,
        OptimizationSubmissionResult? optimization,
        TddftSubmissionResult? tddft,
        CancellationToken cancellationToken)
    {
        result.Succeeded = false;
        result.Error = error;
        result.FailureStage = stage;
        result.FailureCode = failureCode;

        AbsorptionSpectrumRecoveryContext context =
            new AbsorptionSpectrumRecoveryContext();
        context.Request = request;
        context.Stage = stage;
        context.FailureCode = failureCode;
        context.Error = error;
        context.Optimization = optimization;
        context.Tddft = tddft;
        result.RecoveryPlan =
            await _recoveryFramework.PlanAsync(
                context,
                cancellationToken);
    }

    private static CalculationSpec CreateOptimizationSpec(
        CalculationSpec tddftSpec,
        AbsorptionSpectrumMode mode)
    {
        CalculationSpec optimization =
            CalculationDefaults.CreateDefaultOptimization();
        optimization.Program = tddftSpec.Program;
        optimization.Method = tddftSpec.Method;
        optimization.Basis = tddftSpec.Basis;
        optimization.Charge = tddftSpec.Charge;
        optimization.Multiplicity = tddftSpec.Multiplicity;
        optimization.ElectronicStateObjective =
            tddftSpec.ElectronicStateObjective;
        optimization.TargetMultiplicity =
            tddftSpec.TargetMultiplicity;
        optimization.TargetStateLabel =
            tddftSpec.TargetStateLabel;
        optimization.Solvent = tddftSpec.Solvent;

        CopyOption(
            tddftSpec,
            optimization,
            CalculationOptionKeys.DispersionCorrection);
        CopyOption(
            tddftSpec,
            optimization,
            CalculationOptionKeys.Symmetry);
        CopyOption(
            tddftSpec,
            optimization,
            CalculationOptionKeys.ScfIterationLimit);

        optimization.ExtraOptions[
            CalculationOptionKeys.OptimizationConstraintMode] =
            mode == AbsorptionSpectrumMode
                .ConstrainedOptimizationThenTddft
                ? "HydrogensOnly"
                : "None";

        return optimization;
    }

    private static void CopyOption(
        CalculationSpec source,
        CalculationSpec target,
        string key)
    {
        string? value;

        if (source.ExtraOptions.TryGetValue(key, out value)
            && !string.IsNullOrWhiteSpace(value))
        {
            target.ExtraOptions[key] = value;
        }
    }

    private static List<ResolvedCalculationParameter>
        FilterOptimizationParameters(
            List<ResolvedCalculationParameter> parameters)
    {
        List<ResolvedCalculationParameter> filtered =
            new List<ResolvedCalculationParameter>();

        for (int index = 0; index < parameters.Count; index++)
        {
            if (!string.Equals(
                    parameters[index].Key,
                    CalculationOptionKeys.TddftStateCount,
                    StringComparison.OrdinalIgnoreCase))
            {
                filtered.Add(parameters[index]);
            }
        }

        return filtered;
    }

    private static string FormatGeometry(
        CanonicalGeometry geometry)
    {
        string text = string.Empty;

        for (int index = 0;
            index < geometry.Atoms.Count;
            index++)
        {
            CanonicalAtom atom = geometry.Atoms[index];

            if (index > 0)
            {
                text = text + Environment.NewLine;
            }

            text = text +
                atom.Element +
                " " +
                atom.X.ToString(
                    "F6",
                    CultureInfo.InvariantCulture) +
                " " +
                atom.Y.ToString(
                    "F6",
                    CultureInfo.InvariantCulture) +
                " " +
                atom.Z.ToString(
                    "F6",
                    CultureInfo.InvariantCulture);
        }

        return text;
    }

    private static string BuildSuccessMessage(
        AbsorptionSpectrumMode mode,
        TddftCalculationResult tddftResult)
    {
        TddftExcitedState? strongest =
            FindStrongestState(tddftResult);
        string modeText;

        if (mode == AbsorptionSpectrumMode.TddftOnly)
        {
            modeText = "已直接完成 TDDFT 单点计算。";
        }
        else if (mode
            == AbsorptionSpectrumMode
                .ConstrainedOptimizationThenTddft)
        {
            modeText =
                "已完成冻结非氢原子的几何优化和 TDDFT 单点计算。";
        }
        else
        {
            modeText =
                "已完成几何优化和 TDDFT 单点计算。";
        }

        if (strongest == null)
        {
            return modeText;
        }

        return modeText +
            " 共解析 " +
            tddftResult.ExcitedStates.Count.ToString(
                CultureInfo.InvariantCulture) +
            " 个激发态，最强振子强度对应 " +
            strongest.WavelengthNm.ToString(
                "G10",
                CultureInfo.InvariantCulture) +
            " nm。";
    }

    private static TddftExcitedState? FindStrongestState(
        TddftCalculationResult result)
    {
        if (result.ExcitedStates.Count == 0)
        {
            return null;
        }

        TddftExcitedState strongest =
            result.ExcitedStates[0];

        for (int index = 1;
            index < result.ExcitedStates.Count;
            index++)
        {
            if (result.ExcitedStates[index]
                    .OscillatorStrength
                > strongest.OscillatorStrength)
            {
                strongest = result.ExcitedStates[index];
            }
        }

        return strongest;
    }
}
