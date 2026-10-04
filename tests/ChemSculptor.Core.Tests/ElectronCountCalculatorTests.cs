using ChemSculptor.Compute;
using ChemSculptor.InputProcessor;
using ChemSculptor.InputProcessor.Chemistry;

namespace ChemSculptor.Core.Tests;

/// <summary>电子数和默认自旋多重度测试。</summary>
public class ElectronCountCalculatorTests
{
    /// <summary>验证中性氧分子包含 16 个电子，默认多重度为 1。</summary>
    [Fact]
    public void CalculatesNeutralOxygenMultiplicity()
    {
        int electronCount;
        string error;
        bool calculated = ElectronCountCalculator.TryCalculate(
            CreateOxygenAtoms(),
            0,
            out electronCount,
            out error);

        Assert.True(calculated);
        Assert.Equal(string.Empty, error);
        Assert.Equal(16, electronCount);
        Assert.Equal(
            1,
            CalculationDefaults.GetDefaultMultiplicityFromElectronCount(
                electronCount));
    }

    /// <summary>验证带正电氧分子的奇数电子默认多重度为 2。</summary>
    [Fact]
    public void CalculatesCationOxygenMultiplicity()
    {
        int electronCount;
        string error;
        bool calculated = ElectronCountCalculator.TryCalculate(
            CreateOxygenAtoms(),
            1,
            out electronCount,
            out error);

        Assert.True(calculated);
        Assert.Equal(string.Empty, error);
        Assert.Equal(15, electronCount);
        Assert.Equal(
            2,
            CalculationDefaults.GetDefaultMultiplicityFromElectronCount(
                electronCount));
    }

    /// <summary>验证负电荷会增加电子数。</summary>
    [Fact]
    public void NegativeChargeIncreasesElectronCount()
    {
        int electronCount;
        string error;
        bool calculated = ElectronCountCalculator.TryCalculate(
            CreateOxygenAtoms(),
            -1,
            out electronCount,
            out error);

        Assert.True(calculated);
        Assert.Equal(17, electronCount);
        Assert.Equal(
            2,
            CalculationDefaults.GetDefaultMultiplicityFromElectronCount(
                electronCount));
    }

    /// <summary>验证无法识别元素时明确失败。</summary>
    [Fact]
    public void RejectsUnknownElement()
    {
        List<GeometryAtom> atoms = new List<GeometryAtom>();
        GeometryAtom unknown = new GeometryAtom();
        unknown.Element = "Zz";
        atoms.Add(unknown);

        int electronCount;
        string error;
        bool calculated = ElectronCountCalculator.TryCalculate(
            atoms,
            0,
            out electronCount,
            out error);

        Assert.False(calculated);
        Assert.Equal(0, electronCount);
        Assert.Contains("无法识别元素符号", error);
    }

    /// <summary>验证用户显式设置的多重度不会被默认规则覆盖。</summary>
    [Fact]
    public void DoesNotOverrideUserMultiplicity()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();
        CalculationParameter parameter =
            FindParameter(spec, "multiplicity");
        parameter.CurrentValue = "3";
        parameter.Source = ParameterSource.User;
        spec.Multiplicity = 3;

        bool applied =
            CalculationDefaults.ApplyDefaultMultiplicityFromElectronCount(
                spec,
                16);

        Assert.False(applied);
        Assert.Equal(3, spec.Multiplicity);
        Assert.Equal("3", parameter.CurrentValue);
    }

    /// <summary>验证默认规则会更新方案和参数来源。</summary>
    [Fact]
    public void AppliesCalculatedMultiplicityWhenStillDefault()
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultSinglePoint();

        bool applied =
            CalculationDefaults.ApplyDefaultMultiplicityFromElectronCount(
                spec,
                15);
        CalculationParameter parameter =
            FindParameter(spec, "multiplicity");

        Assert.True(applied);
        Assert.Equal(2, spec.Multiplicity);
        Assert.Equal("2", parameter.CurrentValue);
        Assert.Equal(ParameterSource.System, parameter.Source);
    }

    private static List<GeometryAtom> CreateOxygenAtoms()
    {
        List<GeometryAtom> atoms = new List<GeometryAtom>();

        GeometryAtom first = new GeometryAtom();
        first.Element = "O";
        atoms.Add(first);

        GeometryAtom second = new GeometryAtom();
        second.Element = "O";
        atoms.Add(second);

        return atoms;
    }

    private static CalculationParameter FindParameter(
        CalculationSpec spec,
        string name)
    {
        for (int index = 0; index < spec.Parameters.Count; index++)
        {
            CalculationParameter parameter = spec.Parameters[index];

            if (string.Equals(
                parameter.Name,
                name,
                StringComparison.OrdinalIgnoreCase))
            {
                return parameter;
            }
        }

        throw new InvalidOperationException("没有找到计算参数：" + name);
    }
}
