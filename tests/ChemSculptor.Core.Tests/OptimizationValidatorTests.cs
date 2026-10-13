using ChemSculptor.Compute;
using ChemSculptor.InputProcessor.GeometryIntake;
using ChemSculptor.Skills.Common.CalculationResultValidation;

namespace ChemSculptor.Core.Tests;

/// <summary>优化结果验证器测试。</summary>
public class OptimizationValidatorTests
{
    /// <summary>验证完整收敛的优化结果可以通过。</summary>
    [Fact]
    public void ValidOptimizationResultPasses()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "optimization.log");

        try
        {
            File.WriteAllText(outputPath, "Normal termination");
            CalculationResultValidationRequest request =
                CreateRequest(outputPath, true);
            CalculationResultValidationResult result =
                CreateService().Validate(request);

            Assert.True(result.Passed);
            Assert.Equal(
                CalculationValidationStatus.Passed,
                result.Report.Status);
            Assert.Empty(result.Report.Issues);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证未达标时报告明确指出最大力未通过。</summary>
    [Fact]
    public void MaximumForceFailureIdentifiesExactItem()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "optimization.log");

        try
        {
            File.WriteAllText(outputPath, "Normal termination");
            CalculationResultValidationRequest request =
                CreateRequest(outputPath, false);
            request.OptimizationResult!.FinalConvergence!
                .MaximumForce = 0.010000;
            request.OptimizationResult!.FinalConvergence!
                .MaximumForceThreshold = 0.000450;
            request.OptimizationResult!.FinalConvergence!
                .MaximumForceConverged = false;

            CalculationResultValidationResult result =
                CreateService().Validate(request);

            Assert.False(result.Passed);
            CalculationValidationIssue? issue =
                FindIssue(
                    result.Report,
                    "optimization.convergence.maximum_force");
            Assert.NotNull(issue);
            Assert.Contains("最大力未达标", issue!.Message);
            Assert.Contains("0.01", issue.Message);
            Assert.Contains("0.00045", issue.Message);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    /// <summary>验证步骤级 SCF 和能量问题会被逐项报告。</summary>
    [Fact]
    public void StepFailuresIdentifyStepNumber()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "optimization.log");

        try
        {
            File.WriteAllText(outputPath, "Normal termination");
            CalculationResultValidationRequest request =
                CreateRequest(outputPath, true);
            request.OptimizationResult!.Trajectory[0]
                .ScfConverged = false;
            request.OptimizationResult!.Trajectory[0]
                .Energy = null;

            CalculationResultValidationResult result =
                CreateService().Validate(request);

            Assert.False(result.Passed);
            Assert.NotNull(
                FindIssue(
                    result.Report,
                    "optimization.step.1.scf_converged"));
            Assert.NotNull(
                FindIssue(
                    result.Report,
                    "optimization.step.1.energy_present"));
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static CalculationValidationService CreateService()
    {
        List<ICalculationResultValidator> validators =
            new List<ICalculationResultValidator>();
        validators.Add(new OptimizationStepValidator());
        validators.Add(new OptimizationResultValidator());
        validators.Add(new OptimizationConvergenceValidator());
        return new CalculationValidationService(validators);
    }

    private static CalculationResultValidationRequest CreateRequest(
        string outputPath,
        bool converged)
    {
        CalculationSpec spec =
            CalculationDefaults.CreateDefaultOptimization();

        CalculationJob job = new CalculationJob();
        job.JobId = "job-optimization-validation";
        job.Spec = spec;

        OptimizationResult result =
            new OptimizationResult();
        result.JobId = job.JobId;
        result.Program = spec.Program;
        result.Method = spec.Method;
        result.Basis = spec.Basis;
        result.Charge = spec.Charge;
        result.Multiplicity = spec.Multiplicity;
        result.NormalTermination = true;
        result.OptimizationConverged = converged;
        result.StepCount = 1;
        result.FinalEnergy = -76.200000;
        result.OutputFilePath = outputPath;

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
        convergence.MaximumForce = 0.000100;
        convergence.MaximumForceThreshold = 0.000450;
        convergence.MaximumForceConverged = converged;
        convergence.RmsForce = 0.000040;
        convergence.RmsForceThreshold = 0.000300;
        convergence.RmsForceConverged = true;
        convergence.MaximumDisplacement = 0.000300;
        convergence.MaximumDisplacementThreshold = 0.001800;
        convergence.MaximumDisplacementConverged = true;
        convergence.RmsDisplacement = 0.000100;
        convergence.RmsDisplacementThreshold = 0.001200;
        convergence.RmsDisplacementConverged = true;
        result.FinalConvergence = convergence;

        OptimizationStep step = new OptimizationStep();
        step.StepIndex = 1;
        step.Geometry = geometry;
        step.Energy = -76.200000;
        step.ScfConverged = true;
        step.ScfIterations = 5;
        step.Convergence = convergence;
        result.Trajectory.Add(step);

        CalculationResultValidationRequest request =
            new CalculationResultValidationRequest();
        request.Job = job;
        request.OptimizationResult = result;
        return request;
    }

    private static CalculationValidationIssue? FindIssue(
        CalculationValidationReport report,
        string code)
    {
        for (int index = 0;
            index < report.Issues.Count;
            index++)
        {
            if (string.Equals(
                report.Issues[index].Code,
                code,
                StringComparison.OrdinalIgnoreCase))
            {
                return report.Issues[index];
            }
        }

        return null;
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
