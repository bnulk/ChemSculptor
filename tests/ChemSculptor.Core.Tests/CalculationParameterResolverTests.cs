using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>计算参数解析器测试。</summary>
public class CalculationParameterResolverTests
{
    /// <summary>验证默认值、用户值和请求默认值会合并到同一方案。</summary>
    [Fact]
    public void ResolvesFrozenParametersIntoSpec()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        List<CalculationParameterIntent> intents =
            new List<CalculationParameterIntent>();
        intents.Add(CreateIntent(
            CalculationOptionKeys.Method,
            "CAM-B3LYP"));
        intents.Add(CreateIntent(
            CalculationOptionKeys.Basis,
            "6-31G*"));
        intents.Add(CreateKeyOnlyIntent(
            CalculationOptionKeys.DispersionCorrection,
            "加色散"));
        intents.Add(CreateIntent(
            CalculationOptionKeys.Symmetry,
            "Ignore",
            "nosymm"));
        intents.Add(CreateIntent(
            CalculationOptionKeys.ScfIterationLimit,
            "300",
            "SCF 最多 300"));
        intents.Add(CreateIntent(
            CalculationOptionKeys.Solvent,
            "Water",
            "水溶剂"));
        intents.Add(CreateIntent(
            CalculationOptionKeys.Charge,
            "0",
            "电荷 0"));
        intents.Add(CreateIntent(
            CalculationOptionKeys.Multiplicity,
            "3",
            "三重态"));

        CalculationParameterResolutionResult result =
            new CalculationParameterResolver().Resolve(
                spec,
                intents,
                CreateContext(16));

        Assert.Equal(
            CalculationParameterResolutionStatus.Resolved,
            result.Status);
        Assert.Equal("Gaussian 16", result.Spec.Program);
        Assert.Equal("CAM-B3LYP", result.Spec.Method);
        Assert.Equal("6-31G*", result.Spec.Basis);
        Assert.Equal(
            "GD3BJ",
            result.Spec.ExtraOptions[
                CalculationOptionKeys.DispersionCorrection]);
        Assert.Equal(
            "Ignore",
            result.Spec.ExtraOptions[
                CalculationOptionKeys.Symmetry]);
        Assert.Equal(
            "300",
            result.Spec.ExtraOptions[
                CalculationOptionKeys.ScfIterationLimit]);
        Assert.Equal("Water", result.Spec.Solvent);
        Assert.Equal(0, result.Spec.Charge);
        Assert.Equal(3, result.Spec.Multiplicity);
        Assert.Contains(
            result.AppliedParameters,
            parameter =>
                parameter.Key
                    == CalculationOptionKeys.DispersionCorrection
                && parameter.Value == "GD3BJ"
                && parameter.RawText == "加色散");
    }

    /// <summary>验证 KeyOnly 对称性会生成问题。</summary>
    [Fact]
    public void RequiresClarificationForKeyOnlySymmetry()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        List<CalculationParameterIntent> intents =
            new List<CalculationParameterIntent>();
        intents.Add(CreateKeyOnlyIntent(
            CalculationOptionKeys.Symmetry,
            "对称性"));

        CalculationParameterResolutionResult result =
            new CalculationParameterResolver().Resolve(
                spec,
                intents,
                CreateContext(16));

        Assert.Equal(
            CalculationParameterResolutionStatus.NeedsClarification,
            result.Status);
        Assert.Contains(
            result.Questions,
            question =>
                question.ParameterKey
                    == CalculationOptionKeys.Symmetry);
    }

    /// <summary>验证同一参数的不同值会产生冲突问题。</summary>
    [Fact]
    public void RequiresClarificationForConflictingValues()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        List<CalculationParameterIntent> intents =
            new List<CalculationParameterIntent>();
        intents.Add(CreateIntent(
            CalculationOptionKeys.DispersionCorrection,
            "GD3"));
        intents.Add(CreateIntent(
            CalculationOptionKeys.DispersionCorrection,
            "GD3BJ"));

        CalculationParameterResolutionResult result =
            new CalculationParameterResolver().Resolve(
                spec,
                intents,
                CreateContext(16));

        Assert.Equal(
            CalculationParameterResolutionStatus.NeedsClarification,
            result.Status);
        Assert.Contains(
            result.Questions,
            question =>
                question.QuestionId.Contains(
                    "parameter.conflict.same-key",
                    StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>验证多重度与默认电荷奇偶不一致时优先询问电荷。</summary>
    [Fact]
    public void AsksForChargeWhenMultiplicityParityMismatches()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        List<CalculationParameterIntent> intents =
            new List<CalculationParameterIntent>();
        intents.Add(CreateIntent(
            CalculationOptionKeys.Multiplicity,
            "3",
            "三重态"));

        CalculationParameterResolutionResult result =
            new CalculationParameterResolver().Resolve(
                spec,
                intents,
                CreateContext(9));

        Assert.Equal(
            CalculationParameterResolutionStatus.NeedsClarification,
            result.Status);
        Assert.Contains(
            result.Questions,
            question =>
                question.ParameterKey
                    == CalculationOptionKeys.Charge);
    }

    /// <summary>验证超范围 SCF 迭代会拒绝。</summary>
    [Fact]
    public void RejectsOutOfRangeScfIterationLimit()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        List<CalculationParameterIntent> intents =
            new List<CalculationParameterIntent>();
        intents.Add(CreateIntent(
            CalculationOptionKeys.ScfIterationLimit,
            "1500",
            "SCF 最多 1500"));

        CalculationParameterResolutionResult result =
            new CalculationParameterResolver().Resolve(
                spec,
                intents,
                CreateContext(16));

        Assert.Equal(
            CalculationParameterResolutionStatus.Rejected,
            result.Status);
        Assert.Contains(
            result.Diagnostics,
            diagnostic =>
                diagnostic.Code == "parameter.invalid.range");
    }

    private static CalculationParameterResolutionContext CreateContext(
        int electronCount)
    {
        CalculationParameterResolutionContext context =
            new CalculationParameterResolutionContext();
        context.Engine = CalculationDefaults.DefaultProgram;
        context.TaskType = CalculationTaskType.SinglePoint;
        context.ElectronCount = electronCount;
        return context;
    }

    private static CalculationParameterIntent CreateIntent(
        string key,
        string value)
    {
        return CreateIntent(key, value, value);
    }

    private static CalculationParameterIntent CreateIntent(
        string key,
        string value,
        string rawText)
    {
        CalculationParameterIntent intent =
            new CalculationParameterIntent();
        intent.Key = key;
        intent.Value = value;
        intent.RawText = rawText;
        intent.State =
            CalculationParameterIntentState.ExplicitValue;
        intent.Source = ParameterSource.User;
        intent.Confidence = 1.0;
        return intent;
    }

    private static CalculationParameterIntent CreateKeyOnlyIntent(
        string key,
        string rawText)
    {
        CalculationParameterIntent intent =
            new CalculationParameterIntent();
        intent.Key = key;
        intent.RawText = rawText;
        intent.State =
            CalculationParameterIntentState.KeyOnly;
        intent.Source = ParameterSource.User;
        intent.Confidence = 1.0;
        return intent;
    }
}
