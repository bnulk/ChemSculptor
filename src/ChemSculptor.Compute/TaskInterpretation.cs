using System.Text.RegularExpressions;

namespace ChemSculptor.Compute;

/// <summary>
/// 对用户自然语言文本的解释结果。
/// </summary>
public sealed class InterpretedTask
{
    /// <summary>用户原始文本。</summary>
    public string OriginalText { get; set; } = string.Empty;

    /// <summary>解释出的任务类型；无法识别时为空。</summary>
    public CalculationTaskType? TaskType { get; set; }

    /// <summary>建议使用的工作流标识。</summary>
    public string WorkflowId { get; set; } = string.Empty;

    /// <summary>当前阶段是否支持该任务。</summary>
    public bool IsSupported { get; set; }

    /// <summary>解释置信度。</summary>
    public double Confidence { get; set; }

    /// <summary>电子态研究目标。</summary>
    public ElectronicStateObjective ElectronicStateObjective { get; set; } =
        ElectronicStateObjective.GroundState;

    /// <summary>指定自旋多重度；未指定时为空。</summary>
    public int? TargetMultiplicity { get; set; }

    /// <summary>指定激发态标签；未指定时为空。</summary>
    public string TargetStateLabel { get; set; } = string.Empty;

    /// <summary>解释诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();

    /// <summary>吸收光谱任务的前置处理模式。</summary>
    public AbsorptionSpectrumMode AbsorptionSpectrumMode
    {
        get;
        set;
    } = AbsorptionSpectrumMode.OptimizeThenTddft;

    /// <summary>NTO 激发态选择模式。</summary>
    public NtoSelectionMode NtoSelectionMode { get; set; } =
        NtoSelectionMode.Default;

    /// <summary>NTO 波长范围下限，单位 nm。</summary>
    public double? NtoWavelengthMinNm { get; set; }

    /// <summary>NTO 波长范围上限，单位 nm。</summary>
    public double? NtoWavelengthMaxNm { get; set; }

    /// <summary>NTO 前是否必须先执行吸收光谱 Workflow。</summary>
    public bool RunAbsorptionBeforeNto { get; set; }
}

/// <summary>
/// 用户文本解释器。
/// 当前阶段使用规则解释，后续可替换为大语言模型网关。
/// </summary>
public interface ITaskInterpreter
{
    /// <summary>解释用户原始文本。</summary>
    Task<InterpretedTask> InterpretAsync(
        string text,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 基于规则的文本解释器。
/// 只识别当前阶段支持的任务类型。
/// </summary>
public sealed class RuleBasedTaskInterpreter : ITaskInterpreter
{
    private static readonly Regex NtoWavelengthRangePattern =
        new Regex(
            @"(?<min>\d+(?:\.\d+)?)\s*(?:nm|纳米)?\s*[-–—~至到]\s*(?<max>\d+(?:\.\d+)?)\s*(?:nm|纳米)?",
            RegexOptions.Compiled
            | RegexOptions.IgnoreCase
            | RegexOptions.CultureInvariant);

    /// <summary>单点计算的规则关键词。</summary>
    private const string SinglePointKeyword = "单点";

    /// <summary>单点计算的英文关键词。</summary>
    private const string SinglePointEnglishKeyword = "single point";

    /// <summary>单点计算的缩写关键词。</summary>
    private const string SinglePointAbbreviation = "sp";

    /// <summary>几何优化的中文关键词。</summary>
    private const string OptimizationKeyword = "优化";

    /// <summary>几何优化的英文关键词。</summary>
    private const string OptimizationEnglishKeyword =
        "optimization";

    /// <summary>表示只允许氢原子移动的优化约束关键词。</summary>
    private static readonly string[] OptimizationConstraintKeywords =
        new string[]
        {
            "冻结非氢原子",
            "固定非氢原子",
            "非氢原子冻结",
            "只优化氢原子",
            "仅优化氢原子",
            "只让氢原子移动",
            "freeze non-hydrogen",
            "freeze heavy atoms",
            "hydrogens only"
        };

    /// <summary>吸收光谱任务关键词。</summary>
    private static readonly string[] AbsorptionSpectrumKeywords =
        new string[]
        {
            "吸收光谱",
            "吸收谱",
            "紫外可见",
            "紫外",
            "uv-vis",
            "uv vis",
            "ultraviolet",
            "absorption spectrum"
        };

    /// <summary>表示跳过几何优化的关键词。</summary>
    private static readonly string[] SkipOptimizationKeywords =
        new string[]
        {
            "不优化",
            "无需优化",
            "不需要优化",
            "不做优化",
            "跳过优化",
            "直接tddft",
            "直接td-dft",
            "no optimization",
            "skip optimization"
        };

    /// <summary>Gaussian route 前缀。</summary>
    private const string GaussianRoutePrefix = "#p";

    /// <summary>解释输入文本。</summary>
    public Task<InterpretedTask> InterpretAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        InterpretedTask interpretedTask = new InterpretedTask();
        interpretedTask.OriginalText = text;

        if (string.IsNullOrWhiteSpace(text))
        {
            interpretedTask.IsSupported = false;
            interpretedTask.Confidence = 0.0;
            interpretedTask.Diagnostics.Add("输入文本为空。");
            return Task.FromResult(interpretedTask);
        }

        string normalizedText = text.Trim();

        int ntoKeywordIndex = normalizedText.IndexOf(
            "nto",
            StringComparison.OrdinalIgnoreCase);

        if (ntoKeywordIndex >= 0)
        {
            interpretedTask.TaskType =
                CalculationTaskType.Nto;
            interpretedTask.WorkflowId = "nto";
            interpretedTask.IsSupported = true;
            interpretedTask.Confidence = 1.0;

            bool absorptionRequested =
                ContainsAnyKeyword(
                    normalizedText,
                    AbsorptionSpectrumKeywords);

            if (absorptionRequested)
            {
                bool skipOptimization =
                    ContainsAnyKeyword(
                        normalizedText,
                        SkipOptimizationKeywords);
                bool constrainedOptimization =
                    ContainsAnyKeyword(
                        normalizedText,
                        OptimizationConstraintKeywords);

                if (skipOptimization && constrainedOptimization)
                {
                    interpretedTask.IsSupported = false;
                    interpretedTask.Diagnostics.Add(
                        "“不优化”和“冻结非氢原子”不能同时使用。");
                    return Task.FromResult(
                        interpretedTask);
                }

                interpretedTask.RunAbsorptionBeforeNto = true;

                if (skipOptimization)
                {
                    interpretedTask.AbsorptionSpectrumMode =
                        AbsorptionSpectrumMode.TddftOnly;
                }
                else if (constrainedOptimization)
                {
                    interpretedTask.AbsorptionSpectrumMode =
                        AbsorptionSpectrumMode
                            .ConstrainedOptimizationThenTddft;
                }
                else
                {
                    interpretedTask.AbsorptionSpectrumMode =
                        AbsorptionSpectrumMode.OptimizeThenTddft;
                }
            }

            if (HasAllNtoQualifier(
                normalizedText,
                ntoKeywordIndex))
            {
                interpretedTask.NtoSelectionMode =
                    NtoSelectionMode.All;
            }
            else
            {
                Match range =
                    NtoWavelengthRangePattern.Match(
                        normalizedText);

                if (range.Success)
                {
                    double minimum;
                    double maximum;

                    if (double.TryParse(
                            range.Groups["min"].Value,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo
                                .InvariantCulture,
                            out minimum)
                        && double.TryParse(
                            range.Groups["max"].Value,
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo
                                .InvariantCulture,
                            out maximum)
                        && minimum > 0.0
                        && maximum > 0.0
                        && minimum <= maximum)
                    {
                        interpretedTask.NtoSelectionMode =
                            NtoSelectionMode.WavelengthRange;
                        interpretedTask.NtoWavelengthMinNm =
                            minimum;
                        interpretedTask.NtoWavelengthMaxNm =
                            maximum;
                    }
                    else
                    {
                        interpretedTask.IsSupported = false;
                        interpretedTask.Diagnostics.Add(
                            "NTO 波长范围无效，请使用例如 300-500 nm。");
                        return Task.FromResult(
                            interpretedTask);
                    }
                }
            }

            return Task.FromResult(interpretedTask);
        }

        if (ContainsAnyKeyword(
            normalizedText,
            AbsorptionSpectrumKeywords))
        {
            bool skipOptimization = ContainsAnyKeyword(
                normalizedText,
                SkipOptimizationKeywords);
            bool constrainedOptimization =
                ContainsAnyKeyword(
                    normalizedText,
                    OptimizationConstraintKeywords);

            if (skipOptimization && constrainedOptimization)
            {
                interpretedTask.IsSupported = false;
                interpretedTask.Confidence = 0.0;
                interpretedTask.Diagnostics.Add(
                    "“不优化”和“冻结非氢原子”不能同时使用。");
                return Task.FromResult(interpretedTask);
            }

            interpretedTask.TaskType =
                CalculationTaskType.TdDft;
            interpretedTask.WorkflowId =
                "absorption-spectrum";
            interpretedTask.IsSupported = true;
            interpretedTask.Confidence = 1.0;
            interpretedTask.ElectronicStateObjective =
                ElectronicStateObjective.TargetExcitedState;

            if (skipOptimization)
            {
                interpretedTask.AbsorptionSpectrumMode =
                    AbsorptionSpectrumMode.TddftOnly;
            }
            else if (constrainedOptimization)
            {
                interpretedTask.AbsorptionSpectrumMode =
                    AbsorptionSpectrumMode
                        .ConstrainedOptimizationThenTddft;
            }
            else
            {
                interpretedTask.AbsorptionSpectrumMode =
                    AbsorptionSpectrumMode.OptimizeThenTddft;
            }

            return Task.FromResult(interpretedTask);
        }

        if (IsGaussianRouteLine(normalizedText))
        {
            // #、#p、#n、#t 只控制输出信息量。
            // 存在 td 时按 TDDFT 处理，opt 时按优化处理，否则按单点任务处理。
            if (ContainsUnsupportedRouteTask(normalizedText))
            {
                interpretedTask.IsSupported = false;
                interpretedTask.Confidence = 0.0;
                interpretedTask.Diagnostics.Add(
                    "当前阶段只支持单点计算 route。");
                return Task.FromResult(interpretedTask);
            }

            if (ContainsKeyword(normalizedText, "opt")
                && ContainsKeyword(normalizedText, "freq"))
            {
                interpretedTask.TaskType =
                    CalculationTaskType.OptimizationFrequency;
                interpretedTask.WorkflowId =
                    "optimization-frequency";
            }
            else if (ContainsKeyword(normalizedText, "td"))
            {
                interpretedTask.TaskType =
                    CalculationTaskType.TdDft;
                interpretedTask.WorkflowId =
                    "tddft-single-point";
                interpretedTask.ElectronicStateObjective =
                    ElectronicStateObjective.TargetExcitedState;
            }
            else if (ContainsKeyword(normalizedText, "opt"))
            {
                interpretedTask.TaskType =
                    CalculationTaskType.Optimization;
                interpretedTask.WorkflowId =
                    "optimization-complete";
            }
            else
            {
                interpretedTask.TaskType =
                    CalculationTaskType.SinglePoint;
                interpretedTask.WorkflowId = "single_point";
            }

            interpretedTask.IsSupported = true;
            interpretedTask.Confidence = 0.9;
            InterpretElectronicState(
                normalizedText,
                interpretedTask);
            return Task.FromResult(interpretedTask);
        }

        if (IsOptimizationFrequencyText(normalizedText))
        {
            interpretedTask.TaskType =
                CalculationTaskType.OptimizationFrequency;
            interpretedTask.WorkflowId =
                "optimization-frequency";
            interpretedTask.IsSupported = true;
            interpretedTask.Confidence = 1.0;
            InterpretElectronicState(
                normalizedText,
                interpretedTask);
            return Task.FromResult(interpretedTask);
        }

        if (ContainsKeyword(normalizedText, "tddft")
            || ContainsKeyword(normalizedText, "td-dft"))
        {
            interpretedTask.TaskType =
                CalculationTaskType.TdDft;
            interpretedTask.WorkflowId =
                "tddft-single-point";
            interpretedTask.IsSupported = true;
            interpretedTask.Confidence = 1.0;
            interpretedTask.ElectronicStateObjective =
                ElectronicStateObjective.TargetExcitedState;
            return Task.FromResult(interpretedTask);
        }

        if (ContainsKeyword(
                normalizedText,
                OptimizationKeyword)
            || ContainsKeyword(
                normalizedText,
                OptimizationEnglishKeyword)
            || ContainsKeyword(normalizedText, "opt")
            || ContainsAnyKeyword(
                normalizedText,
                OptimizationConstraintKeywords))
        {
            interpretedTask.TaskType =
                CalculationTaskType.Optimization;
            interpretedTask.WorkflowId =
                "optimization-complete";
            interpretedTask.IsSupported = true;
            interpretedTask.Confidence = 1.0;
            InterpretElectronicState(
                normalizedText,
                interpretedTask);
            return Task.FromResult(interpretedTask);
        }

        if (ContainsKeyword(normalizedText, SinglePointKeyword)
            || ContainsKeyword(normalizedText, SinglePointEnglishKeyword)
            || ContainsKeyword(normalizedText, SinglePointAbbreviation))
        {
            interpretedTask.TaskType = CalculationTaskType.SinglePoint;
            interpretedTask.WorkflowId = "single_point";
            interpretedTask.IsSupported = true;
            interpretedTask.Confidence = 1.0;
            InterpretElectronicState(
                normalizedText,
                interpretedTask);
            return Task.FromResult(interpretedTask);
        }

        interpretedTask.IsSupported = false;
        interpretedTask.Confidence = 0.0;
        interpretedTask.Diagnostics.Add("当前阶段只支持单点计算。");

        return Task.FromResult(interpretedTask);
    }

    private static bool IsGaussianRouteLine(string text)
    {
        string trimmed = text.TrimStart();

        return trimmed.StartsWith(
            GaussianRoutePrefix,
            StringComparison.OrdinalIgnoreCase)
            || trimmed.StartsWith(
                "#",
                StringComparison.OrdinalIgnoreCase);
    }

    private static bool ContainsUnsupportedRouteTask(string text)
    {
        bool hasFrequency =
            ContainsKeyword(text, "freq")
            || ContainsKeyword(text, "frequency");
        bool hasOptimization =
            ContainsKeyword(text, "opt");

        return (hasFrequency && !hasOptimization)
            || ContainsKeyword(text, "cis")
            || ContainsKeyword(text, "soc")
            || ContainsKeyword(text, "mecp");
    }

    private static bool ContainsKeyword(string text, string keyword)
    {
        int index = text.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
        return index >= 0;
    }

    private static bool ContainsAnyKeyword(
        string text,
        string[] keywords)
    {
        for (int index = 0; index < keywords.Length; index++)
        {
            if (ContainsKeyword(text, keywords[index]))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAllNtoQualifier(
        string text,
        int ntoKeywordIndex)
    {
        if (ntoKeywordIndex <= 0)
        {
            return false;
        }

        string qualifierText =
            text.Substring(0, ntoKeywordIndex);
        return ContainsKeyword(qualifierText, "全部");
    }

    private static bool IsOptimizationFrequencyText(
        string text)
    {
        bool hasOptimization =
            ContainsKeyword(text, "优化")
            || ContainsKeyword(text, "optimization")
            || ContainsKeyword(text, "opt");
        bool hasFrequency =
            ContainsKeyword(text, "振动分析")
            || ContainsKeyword(text, "频率分析")
            || ContainsKeyword(text, "频率")
            || ContainsKeyword(text, "freq")
            || ContainsKeyword(text, "frequency");

        return hasOptimization && hasFrequency;
    }

    private static void InterpretElectronicState(
        string text,
        InterpretedTask interpretedTask)
    {
        bool excitedStateRequested =
            ContainsKeyword(text, "激发态")
            || ContainsKeyword(text, "excited state")
            || ContainsKeyword(text, "td-dft")
            || ContainsKeyword(text, "tddft");

        int multiplicity;
        bool multiplicityFound = TryReadMultiplicity(
            text,
            out multiplicity);

        if (excitedStateRequested)
        {
            interpretedTask.ElectronicStateObjective =
                ElectronicStateObjective.TargetExcitedState;
            interpretedTask.TargetMultiplicity =
                multiplicityFound
                    ? multiplicity
                    : null;
            interpretedTask.TargetStateLabel =
                ResolveTargetStateLabel(
                    text,
                    multiplicityFound,
                    multiplicity);
            return;
        }

        if (multiplicityFound)
        {
            interpretedTask.ElectronicStateObjective =
                ElectronicStateObjective.TargetSpinState;
            interpretedTask.TargetMultiplicity = multiplicity;
            return;
        }

        interpretedTask.ElectronicStateObjective =
            ElectronicStateObjective.GroundState;
    }

    private static bool TryReadMultiplicity(
        string text,
        out int multiplicity)
    {
        if (ContainsKeyword(text, "单重态")
            || ContainsKeyword(text, "singlet"))
        {
            multiplicity = 1;
            return true;
        }

        if (ContainsKeyword(text, "二重态")
            || ContainsKeyword(text, "doublet"))
        {
            multiplicity = 2;
            return true;
        }

        if (ContainsKeyword(text, "三重态")
            || ContainsKeyword(text, "triplet"))
        {
            multiplicity = 3;
            return true;
        }

        if (ContainsKeyword(text, "四重态")
            || ContainsKeyword(text, "quartet"))
        {
            multiplicity = 4;
            return true;
        }

        if (ContainsKeyword(text, "五重态")
            || ContainsKeyword(text, "quintet"))
        {
            multiplicity = 5;
            return true;
        }

        if (ContainsKeyword(text, "六重态")
            || ContainsKeyword(text, "sextet"))
        {
            multiplicity = 6;
            return true;
        }

        if (ContainsKeyword(text, "七重态")
            || ContainsKeyword(text, "septet"))
        {
            multiplicity = 7;
            return true;
        }

        if (ContainsKeyword(text, "八重态")
            || ContainsKeyword(text, "octet"))
        {
            multiplicity = 8;
            return true;
        }

        multiplicity = 0;
        return false;
    }

    private static string ResolveTargetStateLabel(
        string text,
        bool multiplicityFound,
        int multiplicity)
    {
        string upperText = text.ToUpperInvariant();

        if (upperText.IndexOf("T1", StringComparison.Ordinal) >= 0
            || (multiplicityFound && multiplicity == 3))
        {
            return "T1";
        }

        if (upperText.IndexOf("S1", StringComparison.Ordinal) >= 0
            || (multiplicityFound && multiplicity == 1))
        {
            return "S1";
        }

        return string.Empty;
    }
}
