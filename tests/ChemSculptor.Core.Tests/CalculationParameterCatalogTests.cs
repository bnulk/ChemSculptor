using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>规范参数目录测试。</summary>
public class CalculationParameterCatalogTests
{
    /// <summary>验证首期规范参数目录完整且键不重复。</summary>
    [Fact]
    public void ContainsFrozenFirstPhaseDefinitions()
    {
        IReadOnlyList<CalculationParameterDefinition> definitions =
            CalculationParameterCatalog.List();
        string[] requiredKeys =
        {
            CalculationOptionKeys.Engine,
            CalculationOptionKeys.Method,
            CalculationOptionKeys.Basis,
            CalculationOptionKeys.DispersionCorrection,
            CalculationOptionKeys.Symmetry,
            CalculationOptionKeys.RoutePrintLevel,
            CalculationOptionKeys.ScfIterationLimit,
            CalculationOptionKeys.Solvent,
            CalculationOptionKeys.Charge,
            CalculationOptionKeys.Multiplicity
        };

        Assert.Equal(requiredKeys.Length, definitions.Count);
        Assert.Equal(
            requiredKeys.Length,
            definitions
                .Select(definition => definition.Key)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());

        for (int index = 0; index < requiredKeys.Length; index++)
        {
            CalculationParameterDefinition? definition =
                CalculationParameterCatalog.Find(requiredKeys[index]);

            Assert.NotNull(definition);
            Assert.Contains(
                CalculationTaskType.SinglePoint,
                definition.TaskTypes);
            Assert.Contains(
                CalculationDefaults.DefaultProgram,
                definition.Programs);
            Assert.False(definition.RequiresApproval);
        }
    }

    /// <summary>验证关键默认值和允许值。</summary>
    [Fact]
    public void DefinesFrozenDefaultsAndAllowedValues()
    {
        CalculationParameterDefinition dispersion =
            GetRequired(
                CalculationOptionKeys.DispersionCorrection);
        CalculationParameterDefinition symmetry =
            GetRequired(CalculationOptionKeys.Symmetry);
        CalculationParameterDefinition solvent =
            GetRequired(CalculationOptionKeys.Solvent);

        Assert.Equal("GD3BJ", dispersion.DefaultValue);
        Assert.Equal(
            new string[] { "None", "GD3", "GD3BJ" },
            dispersion.AllowedValues);
        Assert.Contains("色散", dispersion.Aliases);

        Assert.Equal("Default", symmetry.DefaultValue);
        Assert.Equal(
            new string[] { "Default", "Use", "Ignore" },
            symmetry.AllowedValues);
        Assert.Contains("nosymm", symmetry.Aliases);

        Assert.Equal("None", solvent.DefaultValue);
        Assert.Equal(
            new string[] { "None", "Water" },
            solvent.AllowedValues);
        Assert.Contains("水", solvent.Aliases);
    }

    private static CalculationParameterDefinition GetRequired(
        string key)
    {
        CalculationParameterDefinition? definition =
            CalculationParameterCatalog.Find(key);

        if (definition == null)
        {
            throw new InvalidOperationException(
                "缺少参数定义：" + key);
        }

        return definition;
    }
}
