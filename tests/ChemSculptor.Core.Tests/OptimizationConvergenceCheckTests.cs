using ChemSculptor.Anomaly.Checks;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Core.Tests;

/// <summary>优化异常检查测试。</summary>
public class OptimizationConvergenceCheckTests
{
    /// <summary>验证优化未收敛时会给出具体异常、证据和摘要。</summary>
    [Fact]
    public async Task ReportsSpecificConvergenceFindings()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "optimization.log");

        try
        {
            File.WriteAllText(outputPath, "Normal termination");
            AnomalyContext context =
                CreateContext(outputPath, true);
            OptimizationResult optimizationResult =
                context.OptimizationResult!;
            optimizationResult.OptimizationConverged = false;
            optimizationResult.FinalConvergence!.Converged = false;
            optimizationResult.FinalConvergence!
                .MaximumForceConverged = false;
            optimizationResult.FinalConvergence!
                .RmsDisplacementConverged = false;

            OptimizationConvergenceCheck check =
                new OptimizationConvergenceCheck();
            AnomalyCheckResult result =
                await check.CheckAsync(context);

            Assert.Equal(
                AnomalyCheckStatus.Finding,
                result.Status);
            Assert.Contains(
                OptimizationAnomalyCodes.NotConverged,
                result.Summary);
            Assert.Contains(
                result.Findings,
                finding =>
                    finding.Code
                        == OptimizationAnomalyCodes
                            .ForceNotConverged);
            Assert.Contains(
                result.Findings,
                finding =>
                    finding.Code
                        == OptimizationAnomalyCodes
                            .DisplacementNotConverged);

            for (int index = 0;
                index < result.Findings.Count;
                index++)
            {
                Assert.NotEmpty(result.Findings[index].Evidence);
                Assert.False(
                    string.IsNullOrWhiteSpace(
                        result.Findings[index].Details["summary"]));
            }
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证单点异常在优化上下文中仍使用公共错误码。</summary>
    [Fact]
    public async Task ReusesCommonSinglePointAnomalyCodes()
    {
        AnomalyContext context =
            CreateContext(string.Empty, false);
        OptimizationResult optimizationResult =
            context.OptimizationResult!;
        optimizationResult.NormalTermination = false;
        optimizationResult.FinalEnergy = null;
        optimizationResult.Trajectory[0].ScfConverged = false;
        optimizationResult.Trajectory[0].ScfIterations = 200;

        OptimizationConvergenceCheck check =
            new OptimizationConvergenceCheck();
        AnomalyCheckResult result =
            await check.CheckAsync(context);

        string[] requiredCodes =
        {
            CommonAnomalyCodes.OutputMissing,
            CommonAnomalyCodes.NormalTerminationMissing,
            CommonAnomalyCodes.EnergyMissing,
            CommonAnomalyCodes.ScfNotConverged,
            CommonAnomalyCodes.ScfMaxCycles
        };

        for (int index = 0;
            index < requiredCodes.Length;
            index++)
        {
            Assert.Contains(
                result.Findings,
                finding =>
                    finding.Code == requiredCodes[index]);
        }
    }

    /// <summary>验证优化过程类异常可以从诊断证据中识别。</summary>
    [Fact]
    public async Task ReportsProcessAnomaliesFromDiagnostics()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "optimization.log");

        try
        {
            File.WriteAllText(outputPath, "Normal termination");
            AnomalyContext context =
                CreateContext(outputPath, true);
            OptimizationResult optimizationResult =
                context.OptimizationResult!;
            optimizationResult.FailureKind =
                OptimizationFailureKind.StepRejected;
            AddDiagnostic(
                optimizationResult,
                "optimization.oscillation",
                "Optimization oscillated.");
            AddDiagnostic(
                optimizationResult,
                "optimization.trust_radius",
                "Trust radius failure.");
            AddDiagnostic(
                optimizationResult,
                "optimization.checkpoint",
                "Checkpoint file is missing.");
            AddDiagnostic(
                optimizationResult,
                "scf.guess_failed",
                "Initial guess failed.");
            optimizationResult.FinalGeometry =
                new CanonicalGeometry();

            OptimizationConvergenceCheck check =
                new OptimizationConvergenceCheck();
            AnomalyCheckResult result =
                await check.CheckAsync(context);

            Assert.Contains(
                result.Findings,
                finding =>
                    finding.Code
                        == OptimizationAnomalyCodes.Oscillation);
            Assert.Contains(
                result.Findings,
                finding =>
                    finding.Code
                        == OptimizationAnomalyCodes.StepRejected);
            Assert.Contains(
                result.Findings,
                finding =>
                    finding.Code
                        == OptimizationAnomalyCodes
                            .TrustRadiusFailure);
            Assert.Contains(
                result.Findings,
                finding =>
                    finding.Code
                        == OptimizationAnomalyCodes
                            .CheckpointMissing);
            Assert.Contains(
                result.Findings,
                finding =>
                    finding.Code
                        == CommonAnomalyCodes.ScfGuessFailed);
            Assert.Contains(
                result.Findings,
                finding =>
                    finding.Code
                        == OptimizationAnomalyCodes
                            .GeometryInvalid);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证优化异常代码保持稳定且不重复。</summary>
    [Fact]
    public void OptimizationAnomalyCodesAreStable()
    {
        string[] codes =
        {
            OptimizationAnomalyCodes.NotConverged,
            OptimizationAnomalyCodes.MaxCyclesExceeded,
            OptimizationAnomalyCodes.ForceNotConverged,
            OptimizationAnomalyCodes.DisplacementNotConverged,
            OptimizationAnomalyCodes.Oscillation,
            OptimizationAnomalyCodes.StepRejected,
            OptimizationAnomalyCodes.TrustRadiusFailure,
            OptimizationAnomalyCodes.CheckpointMissing,
            OptimizationAnomalyCodes.GeometryInvalid
        };

        Assert.Equal(
            "optimization.not_converged",
            OptimizationAnomalyCodes.NotConverged);
        Assert.Equal(
            "optimization.max_cycles_exceeded",
            OptimizationAnomalyCodes.MaxCyclesExceeded);
        Assert.Equal(
            "optimization.force_not_converged",
            OptimizationAnomalyCodes.ForceNotConverged);
        Assert.Equal(
            "optimization.displacement_not_converged",
            OptimizationAnomalyCodes.DisplacementNotConverged);
        Assert.Equal(
            "optimization.oscillation",
            OptimizationAnomalyCodes.Oscillation);
        Assert.Equal(
            "optimization.step_rejected",
            OptimizationAnomalyCodes.StepRejected);
        Assert.Equal(
            "optimization.trust_radius_failure",
            OptimizationAnomalyCodes.TrustRadiusFailure);
        Assert.Equal(
            "optimization.checkpoint_missing",
            OptimizationAnomalyCodes.CheckpointMissing);
        Assert.Equal(
            "optimization.geometry_invalid",
            OptimizationAnomalyCodes.GeometryInvalid);
        Assert.Equal(
            "scf.not_converged",
            CommonAnomalyCodes.ScfNotConverged);
        Assert.Equal(
            "scf.max_cycles",
            CommonAnomalyCodes.ScfMaxCycles);
        Assert.Equal(
            "scf.guess_failed",
            CommonAnomalyCodes.ScfGuessFailed);
        Assert.Equal(
            "output.missing",
            CommonAnomalyCodes.OutputMissing);
        Assert.Equal(
            "normal_termination.missing",
            CommonAnomalyCodes.NormalTerminationMissing);
        Assert.Equal(
            "energy.missing",
            CommonAnomalyCodes.EnergyMissing);
        Assert.Equal(
            codes.Length,
            codes.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    private static AnomalyContext CreateContext(
        string outputPath,
        bool converged)
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultOptimization();

        CalculationJob job = new CalculationJob();
        job.JobId = "job-optimization-anomaly";
        job.Spec = spec;

        OptimizationResult result =
            new OptimizationResult();
        result.JobId = job.JobId;
        result.Program = spec.Program;
        result.Method = spec.Method;
        result.Basis = spec.Basis;
        result.OutputFilePath = outputPath;
        result.NormalTermination = true;
        result.OptimizationConverged = converged;
        result.FinalEnergy = -76.200000;

        CanonicalGeometry geometry =
            new CanonicalGeometry();
        CanonicalAtom atom = new CanonicalAtom();
        atom.Index = 1;
        atom.Element = "O";
        geometry.Atoms.Add(atom);
        result.FinalGeometry = geometry;

        OptimizationConvergence convergence =
            new OptimizationConvergence();
        convergence.Converged = converged;
        convergence.MaximumForceConverged = true;
        convergence.RmsForceConverged = true;
        convergence.MaximumDisplacementConverged = true;
        convergence.RmsDisplacementConverged = true;
        result.FinalConvergence = convergence;

        OptimizationStep step = new OptimizationStep();
        step.StepIndex = 1;
        step.Geometry = geometry;
        step.Energy = -76.200000;
        step.ScfConverged = true;
        step.ScfIterations = 5;
        result.Trajectory.Add(step);

        AnomalyContext context = new AnomalyContext();
        context.Job = job;
        context.OptimizationResult = result;
        return context;
    }

    private static void AddDiagnostic(
        OptimizationResult result,
        string code,
        string message)
    {
        CalculationDiagnostic diagnostic =
            new CalculationDiagnostic();
        diagnostic.Code = code;
        diagnostic.Message = message;
        result.Diagnostics.Add(diagnostic);
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTemporaryRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }
}
