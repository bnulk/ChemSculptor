using System.Globalization;
using ChemSculptor.Compute;

namespace ChemSculptor.Compute.Gaussian;

/// <summary>Gaussian route 关键词构建结果。</summary>
public sealed class GaussianRouteKeywordBuildResult
{
    /// <summary>方法和基组组合。</summary>
    public string MethodAndBasis { get; set; } = string.Empty;

    /// <summary>任务关键词。</summary>
    public string TaskKeyword { get; set; } = string.Empty;

    /// <summary>Route 前缀，控制输出信息量。</summary>
    public string RoutePrefix { get; set; } = string.Empty;

    /// <summary>参数翻译出的 route 关键词。</summary>
    public List<string> Keywords { get; set; } =
        new List<string>();

    /// <summary>完整 route line。</summary>
    public string RouteLine { get; set; } = string.Empty;

    /// <summary>按规范参数记录的翻译结果。</summary>
    public List<ProgramParameterTranslation> Translations
    {
        get;
        set;
    } = new List<ProgramParameterTranslation>();
}

/// <summary>把通用参数翻译为 Gaussian route 关键词。</summary>
public sealed class GaussianRouteKeywordBuilder
{
    /// <summary>构建 route 关键词。</summary>
    public GaussianRouteKeywordBuildResult Build(
        CalculationSpec spec)
    {
        if (spec == null)
        {
            throw new ArgumentNullException(nameof(spec));
        }

        ValidateProgram(spec);
        ValidateTaskType(spec.TaskType);

        GaussianRouteKeywordBuildResult result =
            new GaussianRouteKeywordBuildResult();
        result.MethodAndBasis =
            spec.Method + "/" + spec.Basis;
        AddRoutePrintLevel(spec, result);

        AddTranslation(
            result,
            CalculationOptionKeys.Method,
            spec.Method,
            spec.Method);
        AddTranslation(
            result,
            CalculationOptionKeys.Basis,
            spec.Basis,
            spec.Basis);
        AddTaskKeyword(spec, result);

        AddDispersionKeyword(spec, result);
        AddSymmetryKeyword(spec, result);
        AddScfIterationKeyword(spec, result);
        AddSolventKeyword(spec, result);

        result.RouteLine = BuildRouteLine(result);
        return result;
    }

    private static void ValidateProgram(CalculationSpec spec)
    {
        if (!string.IsNullOrWhiteSpace(spec.Program)
            && !string.Equals(
                spec.Program,
                Gaussian16ProgramAdapter.ProgramNameValue,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "当前翻译器只支持 " +
                Gaussian16ProgramAdapter.ProgramNameValue +
                "。");
        }
    }

    private static void ValidateTaskType(
        CalculationTaskType taskType)
    {
        if (taskType != CalculationTaskType.SinglePoint
            && taskType != CalculationTaskType.Optimization)
        {
            throw new NotSupportedException(
                "当前阶段只支持单点计算和几何优化输入生成。");
        }
    }

    private static void AddTaskKeyword(
        CalculationSpec spec,
        GaussianRouteKeywordBuildResult result)
    {
        if (spec.TaskType == CalculationTaskType.SinglePoint)
        {
            result.TaskKeyword = "SP";
            AddTranslation(
                result,
                "task-type",
                spec.TaskType.ToString(),
                result.TaskKeyword);
            return;
        }

        List<string> optimizationOptions =
            new List<string>();
        AddOptimizationInitialHessian(
            spec,
            optimizationOptions,
            result);
        AddOptimizationConvergence(
            spec,
            optimizationOptions,
            result);
        AddOptimizationRestart(
            spec,
            optimizationOptions,
            result);
        AddOptimizationMaxCycles(
            spec,
            optimizationOptions,
            result);
        result.TaskKeyword =
            BuildOptimizationTaskKeyword(optimizationOptions);
        AddTranslation(
            result,
            "task-type",
            spec.TaskType.ToString(),
            result.TaskKeyword);
    }

    private static void AddOptimizationInitialHessian(
        CalculationSpec spec,
        List<string> optimizationOptions,
        GaussianRouteKeywordBuildResult result)
    {
        string value = ReadExtraOption(
            spec,
            CalculationOptionKeys.OptimizationInitialHessian);

        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(
                value,
                "None",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string option;

        if (string.Equals(
            value,
            "CalcFC",
            StringComparison.OrdinalIgnoreCase))
        {
            option = "CalcFC";
        }
        else if (string.Equals(
            value,
            "ReadFC",
            StringComparison.OrdinalIgnoreCase))
        {
            option = "ReadFC";
        }
        else
        {
            throw new NotSupportedException(
                "不支持的优化初始 Hessian：" + value);
        }

        AddOptimizationOption(
            optimizationOptions,
            option);
        AddTranslation(
            result,
            CalculationOptionKeys.OptimizationInitialHessian,
            value,
            option);
    }

    private static void AddOptimizationConvergence(
        CalculationSpec spec,
        List<string> optimizationOptions,
        GaussianRouteKeywordBuildResult result)
    {
        string value = ReadExtraOption(
            spec,
            CalculationOptionKeys.OptimizationConvergence);

        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(
                value,
                "Default",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string option;

        if (string.Equals(
            value,
            "Tight",
            StringComparison.OrdinalIgnoreCase))
        {
            option = "Tight";
        }
        else if (string.Equals(
            value,
            "VeryTight",
            StringComparison.OrdinalIgnoreCase))
        {
            option = "VeryTight";
        }
        else
        {
            throw new NotSupportedException(
                "不支持的优化收敛标准：" + value);
        }

        AddOptimizationOption(
            optimizationOptions,
            option);
        AddTranslation(
            result,
            CalculationOptionKeys.OptimizationConvergence,
            value,
            option);
    }

    private static void AddOptimizationRestart(
        CalculationSpec spec,
        List<string> optimizationOptions,
        GaussianRouteKeywordBuildResult result)
    {
        string value = ReadExtraOption(
            spec,
            CalculationOptionKeys.OptimizationRestart);

        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(
                value,
                "false",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!string.Equals(
            value,
            "true",
            StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "优化重启值必须是 true 或 false。");
        }

        AddOptimizationOption(
            optimizationOptions,
            "Restart");
        AddTranslation(
            result,
            CalculationOptionKeys.OptimizationRestart,
            value,
            "Restart");
        AddKeyword(
            result,
            CalculationOptionKeys.OptimizationRestart,
            value,
            "geom=check");
        AddKeyword(
            result,
            CalculationOptionKeys.OptimizationRestart,
            value,
            "guess=read");
    }

    private static void AddOptimizationMaxCycles(
        CalculationSpec spec,
        List<string> optimizationOptions,
        GaussianRouteKeywordBuildResult result)
    {
        string value = ReadExtraOption(
            spec,
            CalculationOptionKeys.OptimizationMaxCycles);

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        int maxCycles;

        if (!int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out maxCycles)
            || maxCycles <= 0)
        {
            throw new NotSupportedException(
                "优化最大循环次数必须是正整数。");
        }

        string option =
            "MaxCycles=" +
            maxCycles.ToString(CultureInfo.InvariantCulture);
        AddOptimizationOption(
            optimizationOptions,
            option);
        AddTranslation(
            result,
            CalculationOptionKeys.OptimizationMaxCycles,
            value,
            option);
    }

    private static void AddOptimizationOption(
        List<string> optimizationOptions,
        string option)
    {
        for (int index = 0;
            index < optimizationOptions.Count;
            index++)
        {
            if (string.Equals(
                optimizationOptions[index],
                option,
                StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        optimizationOptions.Add(option);
    }

    private static string BuildOptimizationTaskKeyword(
        List<string> optimizationOptions)
    {
        if (optimizationOptions.Count == 0)
        {
            return "opt";
        }

        if (optimizationOptions.Count == 1)
        {
            return "opt=" + optimizationOptions[0];
        }

        string taskKeyword = "opt=(";

        for (int index = 0;
            index < optimizationOptions.Count;
            index++)
        {
            if (index > 0)
            {
                taskKeyword = taskKeyword + ", ";
            }

            taskKeyword =
                taskKeyword + optimizationOptions[index];
        }

        return taskKeyword + ")";
    }

    private static void AddDispersionKeyword(
        CalculationSpec spec,
        GaussianRouteKeywordBuildResult result)
    {
        string value = ReadExtraOption(
            spec,
            CalculationOptionKeys.DispersionCorrection);

        if (string.IsNullOrWhiteSpace(value))
        {
            value = "GD3BJ";
        }

        if (string.Equals(
                value,
                "None",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(
            value,
            "GD3",
            StringComparison.OrdinalIgnoreCase))
        {
            AddKeyword(
                result,
                CalculationOptionKeys.DispersionCorrection,
                value,
                "em=GD3");
            return;
        }

        if (string.Equals(
            value,
            "GD3BJ",
            StringComparison.OrdinalIgnoreCase))
        {
            AddKeyword(
                result,
                CalculationOptionKeys.DispersionCorrection,
                value,
                "em=GD3BJ");
            return;
        }

        throw new NotSupportedException(
            "不支持的色散校正：" + value);
    }

    private static void AddRoutePrintLevel(
        CalculationSpec spec,
        GaussianRouteKeywordBuildResult result)
    {
        string value = ReadExtraOption(
            spec,
            CalculationOptionKeys.RoutePrintLevel);

        if (string.IsNullOrWhiteSpace(value))
        {
            value = "Verbose";
        }

        if (string.Equals(
            value,
            "Default",
            StringComparison.OrdinalIgnoreCase))
        {
            result.RoutePrefix = "#";
        }
        else if (string.Equals(
            value,
            "Terse",
            StringComparison.OrdinalIgnoreCase))
        {
            result.RoutePrefix = "#t";
        }
        else if (string.Equals(
            value,
            "Normal",
            StringComparison.OrdinalIgnoreCase))
        {
            result.RoutePrefix = "#n";
        }
        else if (string.Equals(
            value,
            "Verbose",
            StringComparison.OrdinalIgnoreCase))
        {
            result.RoutePrefix = "#p";
        }
        else
        {
            throw new NotSupportedException(
                "不支持的 route 输出信息量：" + value);
        }

        AddTranslation(
            result,
            CalculationOptionKeys.RoutePrintLevel,
            value,
            result.RoutePrefix);
    }

    private static void AddSymmetryKeyword(
        CalculationSpec spec,
        GaussianRouteKeywordBuildResult result)
    {
        string value = ReadExtraOption(
            spec,
            CalculationOptionKeys.Symmetry);

        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(
                value,
                "Default",
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                value,
                "Use",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(
            value,
            "Ignore",
            StringComparison.OrdinalIgnoreCase))
        {
            AddKeyword(
                result,
                CalculationOptionKeys.Symmetry,
                value,
                "nosymm");
            return;
        }

        throw new NotSupportedException(
            "不支持的对称性模式：" + value);
    }

    private static void AddScfIterationKeyword(
        CalculationSpec spec,
        GaussianRouteKeywordBuildResult result)
    {
        string value = ReadExtraOption(
            spec,
            CalculationOptionKeys.ScfIterationLimit);

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        int iterationLimit;

        if (!int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out iterationLimit)
            || iterationLimit <= 0)
        {
            throw new NotSupportedException(
                "SCF 迭代上限必须是正整数。");
        }

        AddKeyword(
            result,
            CalculationOptionKeys.ScfIterationLimit,
            value,
            "scfcyc=" +
            iterationLimit.ToString(
                CultureInfo.InvariantCulture));
    }

    private static void AddSolventKeyword(
        CalculationSpec spec,
        GaussianRouteKeywordBuildResult result)
    {
        string value = spec.Solvent;

        if (string.IsNullOrWhiteSpace(value)
            || string.Equals(
                value,
                "None",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (string.Equals(
            value,
            "Water",
            StringComparison.OrdinalIgnoreCase))
        {
            AddKeyword(
                result,
                CalculationOptionKeys.Solvent,
                value,
                "SCRF=(Solvent=Water)");
            return;
        }

        throw new NotSupportedException(
            "不支持的溶剂：" + value);
    }

    private static string ReadExtraOption(
        CalculationSpec spec,
        string key)
    {
        string? value;

        if (spec.ExtraOptions.TryGetValue(key, out value))
        {
            return value;
        }

        return string.Empty;
    }

    private static void AddKeyword(
        GaussianRouteKeywordBuildResult result,
        string key,
        string value,
        string keyword)
    {
        result.Keywords.Add(keyword);
        AddTranslation(result, key, value, keyword);
    }

    private static void AddTranslation(
        GaussianRouteKeywordBuildResult result,
        string key,
        string value,
        string keyword)
    {
        ProgramParameterTranslation translation =
            new ProgramParameterTranslation();
        translation.Key = key;
        translation.Value = value;
        translation.RawKeyword = keyword;
        translation.TranslatorName =
            "GaussianRouteKeywordBuilder";
        translation.TranslatorVersion = "1.1.0";
        result.Translations.Add(translation);
    }

    private static string BuildRouteLine(
        GaussianRouteKeywordBuildResult result)
    {
        string routeLine =
            result.RoutePrefix +
            " " +
            result.MethodAndBasis +
            " " +
            result.TaskKeyword;

        for (int index = 0;
            index < result.Keywords.Count;
            index++)
        {
            routeLine =
                routeLine +
                " " +
                result.Keywords[index];
        }

        return routeLine;
    }
}
