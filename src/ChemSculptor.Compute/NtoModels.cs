namespace ChemSculptor.Compute;

/// <summary>NTO 激发态选择模式。</summary>
public enum NtoSelectionMode
{
    /// <summary>按振子强度选择最强吸收。</summary>
    Default,

    /// <summary>在指定波长范围内选择。</summary>
    WavelengthRange,

    /// <summary>选择全部激发态，包括暗态。</summary>
    All
}

/// <summary>NTO 状态选择结果。</summary>
public enum NtoSelectionOutcome
{
    /// <summary>已选择可计算的激发态。</summary>
    Selected,

    /// <summary>没有振子强度大于零的激发态。</summary>
    NoPositiveOscillatorStrength
}

/// <summary>NTO 工作流状态。</summary>
public enum NtoWorkflowStatus
{
    /// <summary>已完成。</summary>
    Completed,

    /// <summary>部分 NTO 作业失败。</summary>
    Partial,

    /// <summary>没有符合条件的状态。</summary>
    NoPositiveOscillatorStrength,

    /// <summary>工作流失败。</summary>
    Failed
}

/// <summary>NTO 计算请求。</summary>
public sealed class NtoRequest
{
    /// <summary>客户端会话标识。</summary>
    public string SessionId { get; set; } = string.Empty;

    /// <summary>科研目标。</summary>
    public string Goal { get; set; } = string.Empty;

    /// <summary>源 TDDFT 作业标识。</summary>
    public string SourceTddftJobId { get; set; } = string.Empty;

    /// <summary>激发态选择模式。</summary>
    public NtoSelectionMode SelectionMode { get; set; } =
        NtoSelectionMode.Default;

    /// <summary>波长范围下限，单位 nm。</summary>
    public double? WavelengthMinNm { get; set; }

    /// <summary>波长范围上限，单位 nm。</summary>
    public double? WavelengthMaxNm { get; set; }
}

/// <summary>NTO 选中的激发态。</summary>
public sealed class NtoSelectedState
{
    /// <summary>稳定状态标识。</summary>
    public string StateId { get; set; } = string.Empty;

    /// <summary>源 TDDFT 输出中的激发态编号。</summary>
    public int StateIndex { get; set; }

    /// <summary>激发态标签。</summary>
    public string StateLabel { get; set; } = string.Empty;

    /// <summary>激发能，单位 eV。</summary>
    public double ExcitationEnergyEv { get; set; }

    /// <summary>波长，单位 nm。</summary>
    public double WavelengthNm { get; set; }

    /// <summary>振子强度。</summary>
    public double OscillatorStrength { get; set; }
}

/// <summary>单个激发态的 NTO 计算请求。</summary>
public sealed class NtoTransitionRequest
{
    /// <summary>源 TDDFT 作业。</summary>
    public CalculationJob SourceJob { get; set; } =
        new CalculationJob();

    /// <summary>源 TDDFT 方案。</summary>
    public CalculationSpec SourceSpec { get; set; } =
        new CalculationSpec();

    /// <summary>源 TDDFT 输入文件路径。</summary>
    public string SourceInputPath { get; set; } = string.Empty;

    /// <summary>源 TDDFT 检查点路径。</summary>
    public string SourceCheckpointPath { get; set; } = string.Empty;

    /// <summary>被选中的激发态。</summary>
    public NtoSelectedState SelectedState { get; set; } =
        new NtoSelectedState();

    /// <summary>面向用户的科研目标。</summary>
    public string Goal { get; set; } = string.Empty;
}

/// <summary>程序专用 NTO 输入写入请求。</summary>
public sealed class NtoInputWriteRequest
{
    /// <summary>源 TDDFT 输入文件路径。</summary>
    public string SourceInputPath { get; set; } = string.Empty;

    /// <summary>目标 NTO 输入文件路径。</summary>
    public string TargetInputPath { get; set; } = string.Empty;

    /// <summary>目标检查点文件名。</summary>
    public string CheckpointFileName { get; set; } = string.Empty;

    /// <summary>源激发态编号。</summary>
    public int TransitionIndex { get; set; }
}

/// <summary>单个激发态的 NTO 计算结果。</summary>
public sealed class NtoTransitionResult
{
    /// <summary>是否成功。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>派生 NTO 作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>源 TDDFT 作业标识。</summary>
    public string SourceJobId { get; set; } = string.Empty;

    /// <summary>源激发态编号。</summary>
    public int StateIndex { get; set; }

    /// <summary>源激发态标签。</summary>
    public string StateLabel { get; set; } = string.Empty;

    /// <summary>程序是否正常结束。</summary>
    public bool NormalTermination { get; set; }

    /// <summary>是否识别到 NTO 输出。</summary>
    public bool NtoFound { get; set; }

    /// <summary>NTO 输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>产物清单。</summary>
    public List<CalculationArtifactDescriptor> Artifacts
    {
        get;
        set;
    } = new List<CalculationArtifactDescriptor>();

    /// <summary>诊断信息。</summary>
    public List<CalculationDiagnostic> Diagnostics { get; set; } =
        new List<CalculationDiagnostic>();
}

/// <summary>NTO 状态选择结果。</summary>
public sealed class NtoStateSelectionResult
{
    /// <summary>选择结果类型。</summary>
    public NtoSelectionOutcome Outcome { get; set; }

    /// <summary>选中的状态。</summary>
    public List<NtoSelectedState> States { get; set; } =
        new List<NtoSelectedState>();

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>NTO 工作流结果。</summary>
public sealed class NtoWorkflowResult
{
    /// <summary>工作流是否完成。</summary>
    public bool Succeeded { get; set; }

    /// <summary>工作流状态。</summary>
    public NtoWorkflowStatus Status { get; set; }

    /// <summary>面向用户的说明。</summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>源 TDDFT 作业标识。</summary>
    public string SourceTddftJobId { get; set; } = string.Empty;

    /// <summary>选择模式。</summary>
    public NtoSelectionMode SelectionMode { get; set; }

    /// <summary>选中的状态。</summary>
    public List<NtoSelectedState> SelectedStates { get; set; } =
        new List<NtoSelectedState>();

    /// <summary>每个状态的 NTO 结果。</summary>
    public List<NtoTransitionResult> TransitionResults
    {
        get;
        set;
    } = new List<NtoTransitionResult>();
}

/// <summary>NTO 状态选择器。</summary>
public static class NtoStateSelector
{
    /// <summary>按客户端规则选择 NTO 状态。</summary>
    public static NtoStateSelectionResult Select(
        TddftCalculationResult source,
        NtoSelectionMode mode,
        double? wavelengthMinNm,
        double? wavelengthMaxNm)
    {
        if (source == null)
        {
            throw new ArgumentNullException(nameof(source));
        }

        NtoStateSelectionResult result =
            new NtoStateSelectionResult();

        if (mode == NtoSelectionMode.All)
        {
            for (int index = 0;
                index < source.ExcitedStates.Count;
                index++)
            {
                result.States.Add(
                    ToSelectedState(source.ExcitedStates[index]));
            }

            result.Outcome = result.States.Count > 0
                ? NtoSelectionOutcome.Selected
                : NtoSelectionOutcome
                    .NoPositiveOscillatorStrength;
            result.Message = result.States.Count > 0
                ? "选择全部激发态，包括暗态。"
                : "源 TDDFT 结果中没有激发态。";
            return result;
        }

        List<TddftExcitedState> candidates =
            new List<TddftExcitedState>();

        for (int index = 0;
            index < source.ExcitedStates.Count;
            index++)
        {
            TddftExcitedState state =
                source.ExcitedStates[index];

            if (state.OscillatorStrength <= 0.0)
            {
                continue;
            }

            if (mode == NtoSelectionMode.WavelengthRange)
            {
                if (wavelengthMinNm.HasValue
                    && state.WavelengthNm
                        < wavelengthMinNm.Value)
                {
                    continue;
                }

                if (wavelengthMaxNm.HasValue
                    && state.WavelengthNm
                        > wavelengthMaxNm.Value)
                {
                    continue;
                }
            }

            candidates.Add(state);
        }

        if (candidates.Count == 0)
        {
            result.Outcome =
                NtoSelectionOutcome
                    .NoPositiveOscillatorStrength;
            result.Message =
                mode == NtoSelectionMode.WavelengthRange
                    ? "指定波长范围内没有 f > 0 的激发态。"
                    : "源 TDDFT 结果中没有 f > 0 的激发态。";
            return result;
        }

        candidates.Sort(CompareByOscillatorStrength);
        int limit = Math.Min(5, candidates.Count);
        List<TddftExcitedState> top =
            candidates.GetRange(0, limit);
        double maximum =
            top[0].OscillatorStrength;
        double minimumPositive =
            top[top.Count - 1].OscillatorStrength;
        bool filterByMagnitude =
            top.Count >= 2
            && maximum / minimumPositive > 10.0;

        for (int index = 0;
            index < top.Count;
            index++)
        {
            TddftExcitedState state = top[index];

            if (filterByMagnitude
                && state.OscillatorStrength
                    < maximum / 10.0)
            {
                continue;
            }

            result.States.Add(ToSelectedState(state));
        }

        result.Outcome = NtoSelectionOutcome.Selected;
        result.Message =
            "已按振子强度选择 " +
            result.States.Count.ToString() +
            " 个激发态。";
        return result;
    }

    private static int CompareByOscillatorStrength(
        TddftExcitedState left,
        TddftExcitedState right)
    {
        int result = right.OscillatorStrength
            .CompareTo(left.OscillatorStrength);

        if (result != 0)
        {
            return result;
        }

        return left.StateIndex.CompareTo(right.StateIndex);
    }

    private static NtoSelectedState ToSelectedState(
        TddftExcitedState state)
    {
        NtoSelectedState selected = new NtoSelectedState();
        selected.StateIndex = state.StateIndex;
        selected.StateLabel = state.StateLabel;
        selected.ExcitationEnergyEv = state.EnergyEv;
        selected.WavelengthNm = state.WavelengthNm;
        selected.OscillatorStrength =
            state.OscillatorStrength;
        return selected;
    }
}
