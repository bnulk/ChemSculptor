using ChemSculptor.Compute;

namespace ChemSculptor.Core.Tests;

/// <summary>规范计算参数键测试。</summary>
public class CalculationOptionKeysTests
{
    /// <summary>验证外部可见的规范参数键保持稳定且不重复。</summary>
    [Fact]
    public void DefinesStableCanonicalParameterKeys()
    {
        string[] keys =
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
            CalculationOptionKeys.Multiplicity,
            CalculationOptionKeys.OptimizationMaxCycles,
            CalculationOptionKeys.OptimizationConvergence,
            CalculationOptionKeys.OptimizationRestart,
            CalculationOptionKeys.OptimizationInitialHessian
        };

        Assert.Equal("engine", CalculationOptionKeys.Engine);
        Assert.Equal("method", CalculationOptionKeys.Method);
        Assert.Equal("basis", CalculationOptionKeys.Basis);
        Assert.Equal(
            "dispersion-correction",
            CalculationOptionKeys.DispersionCorrection);
        Assert.Equal("symmetry", CalculationOptionKeys.Symmetry);
        Assert.Equal(
            "route-print-level",
            CalculationOptionKeys.RoutePrintLevel);
        Assert.Equal(
            "scf-iteration-limit",
            CalculationOptionKeys.ScfIterationLimit);
        Assert.Equal("solvent", CalculationOptionKeys.Solvent);
        Assert.Equal("charge", CalculationOptionKeys.Charge);
        Assert.Equal(
            "multiplicity",
            CalculationOptionKeys.Multiplicity);
        Assert.Equal(
            "optimization-max-cycles",
            CalculationOptionKeys.OptimizationMaxCycles);
        Assert.Equal(
            "optimization-convergence",
            CalculationOptionKeys.OptimizationConvergence);
        Assert.Equal(
            "optimization-restart",
            CalculationOptionKeys.OptimizationRestart);
        Assert.Equal(
            "optimization-initial-hessian",
            CalculationOptionKeys.OptimizationInitialHessian);
        Assert.Equal(
            keys.Length,
            keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }
}
