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
        result.TaskKeyword = "SP";

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
        AddTranslation(
            result,
            "task-type",
            spec.TaskType.ToString(),
            result.TaskKeyword);

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
        if (taskType != CalculationTaskType.SinglePoint)
        {
            throw new NotSupportedException(
                "当前阶段只支持单点计算输入生成。");
        }
    }

    private static void AddDispersionKeyword(
        CalculationSpec spec,
        GaussianRouteKeywordBuildResult result)
    {
        string value = ReadExtraOption(
            spec,
            CalculationOptionKeys.DispersionCorrection);

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
        translation.TranslatorVersion = "1.0.0";
        result.Translations.Add(translation);
    }

    private static string BuildRouteLine(
        GaussianRouteKeywordBuildResult result)
    {
        string routeLine =
            "#p " +
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
