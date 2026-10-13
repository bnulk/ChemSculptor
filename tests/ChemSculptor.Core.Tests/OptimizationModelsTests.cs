using System.Text.Json;
using System.Text.Json.Serialization;
using ChemSculptor.Compute;
using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Core.Tests;

/// <summary>几何优化模型测试。</summary>
public class OptimizationModelsTests
{
    /// <summary>验证优化结果契约可以 JSON 往返。</summary>
    [Fact]
    public void OptimizationResultRoundTripsThroughJson()
    {
        OptimizationResult result = CreateOptimizationResult();
        JsonSerializerOptions options = CreateJsonOptions();

        string json = JsonSerializer.Serialize(result, options);
        OptimizationResult? restored =
            JsonSerializer.Deserialize<OptimizationResult>(
                json,
                options);

        Assert.NotNull(restored);
        Assert.Equal(result.JobId, restored.JobId);
        Assert.Equal(
            result.NormalTermination,
            restored.NormalTermination);
        Assert.Equal(
            result.OptimizationConverged,
            restored.OptimizationConverged);
        Assert.Equal(result.StepCount, restored.StepCount);
        Assert.Equal(result.FinalEnergy, restored.FinalEnergy);
        Assert.Equal(
            result.FinalGeometry.Atoms.Count,
            restored.FinalGeometry.Atoms.Count);
        Assert.NotNull(restored.FinalConvergence);
        Assert.Equal(
            result.FinalConvergence!.MaximumForce,
            restored.FinalConvergence.MaximumForce);
        Assert.Equal(
            result.FinalConvergence.RmsDisplacement,
            restored.FinalConvergence.RmsDisplacement);
        Assert.Single(restored.Trajectory);
        Assert.Single(restored.Trajectory[0].Gradients);
        Assert.Equal(
            OptimizationFailureKind.OptimizationNotConverged,
            restored.FailureKind);
        Assert.Single(restored.Diagnostics);
        Assert.Single(restored.Artifacts);
    }

    /// <summary>验证优化请求契约可以 JSON 往返。</summary>
    [Fact]
    public void OptimizationRequestRoundTripsThroughJson()
    {
        OptimizationRequest request = new OptimizationRequest();
        request.SessionId = "session-optimization";
        request.GeometryId = "geometry-water";
        request.Goal = "优化水分子";
        request.CoordinateText = "O 0.0 0.0 0.0";
        request.Charge = 0;
        request.Multiplicity = 1;
        request.ResolvedSpec =
            CalculationDefaults.CreateDefaultSinglePoint();
        request.ResolvedSpec.TaskType =
            CalculationTaskType.Optimization;

        JsonSerializerOptions options = CreateJsonOptions();
        string json = JsonSerializer.Serialize(request, options);
        OptimizationRequest? restored =
            JsonSerializer.Deserialize<OptimizationRequest>(
                json,
                options);

        Assert.NotNull(restored);
        Assert.Equal(request.SessionId, restored.SessionId);
        Assert.Equal(request.GeometryId, restored.GeometryId);
        Assert.Equal(request.Goal, restored.Goal);
        Assert.Equal(request.Charge, restored.Charge);
        Assert.Equal(request.Multiplicity, restored.Multiplicity);
        Assert.NotNull(restored.ResolvedSpec);
        Assert.Equal(
            CalculationTaskType.Optimization,
            restored.ResolvedSpec.TaskType);
    }

    private static OptimizationResult CreateOptimizationResult()
    {
        CanonicalGeometry finalGeometry = CreateWaterGeometry();
        OptimizationConvergence convergence =
            new OptimizationConvergence();
        convergence.Converged = false;
        convergence.Criteria = "Default";
        convergence.MaximumForce = 0.0123;
        convergence.RmsForce = 0.0045;
        convergence.MaximumDisplacement = 0.0200;
        convergence.RmsDisplacement = 0.0060;

        OptimizationStep step = new OptimizationStep();
        step.StepIndex = 1;
        step.Geometry = CreateWaterGeometry();
        step.Energy = -76.3801014;
        step.EnergyUnit = "Hartree";
        step.ScfConverged = true;
        step.ScfIterations = 8;
        step.Convergence = convergence;
        OptimizationGradientComponent gradient =
            new OptimizationGradientComponent();
        gradient.AtomIndex = 1;
        gradient.Element = "O";
        gradient.X = 0.001;
        gradient.Y = -0.002;
        gradient.Z = 0.003;
        step.Gradients.Add(gradient);

        CalculationDiagnostic diagnostic =
            new CalculationDiagnostic();
        diagnostic.Severity =
            CalculationDiagnosticSeverity.Warning;
        diagnostic.Code = "optimization.not_converged";
        diagnostic.Message = "优化尚未完全收敛。";
        step.Diagnostics.Add(diagnostic);

        CalculationArtifactDescriptor artifact =
            new CalculationArtifactDescriptor();
        artifact.FileName = "output.log";
        artifact.RelativePath = "output.log";
        artifact.Kind = CalculationArtifactKind.PrimaryOutput;
        artifact.MediaType = "text/plain; charset=utf-8";
        artifact.Length = 1024;
        artifact.Sha256 = "ABCDEF";

        OptimizationResult result = new OptimizationResult();
        result.JobId = "job-optimization";
        result.Program = CalculationDefaults.DefaultProgram;
        result.Method = CalculationDefaults.DefaultMethod;
        result.Basis = CalculationDefaults.DefaultBasis;
        result.Charge = 0;
        result.Multiplicity = 1;
        result.NormalTermination = true;
        result.OptimizationConverged = false;
        result.StepCount = 1;
        result.FinalEnergy = -76.3801014;
        result.FinalGeometry = finalGeometry;
        result.FinalConvergence = convergence;
        result.Trajectory.Add(step);
        result.FailureKind =
            OptimizationFailureKind.OptimizationNotConverged;
        result.OutputFilePath = "output.log";
        result.Diagnostics.Add(diagnostic);
        result.Artifacts.Add(artifact);
        return result;
    }

    private static CanonicalGeometry CreateWaterGeometry()
    {
        CanonicalGeometry geometry = new CanonicalGeometry();
        CanonicalAtom oxygen = new CanonicalAtom();
        oxygen.Index = 1;
        oxygen.Element = "O";
        oxygen.X = 0.000000;
        oxygen.Y = 0.000000;
        oxygen.Z = 0.117300;
        geometry.Atoms.Add(oxygen);

        CanonicalAtom hydrogenOne = new CanonicalAtom();
        hydrogenOne.Index = 2;
        hydrogenOne.Element = "H";
        hydrogenOne.X = 0.000000;
        hydrogenOne.Y = 0.757200;
        hydrogenOne.Z = -0.469200;
        geometry.Atoms.Add(hydrogenOne);

        CanonicalAtom hydrogenTwo = new CanonicalAtom();
        hydrogenTwo.Index = 3;
        hydrogenTwo.Element = "H";
        hydrogenTwo.X = 0.000000;
        hydrogenTwo.Y = -0.757200;
        hydrogenTwo.Z = -0.469200;
        geometry.Atoms.Add(hydrogenTwo);
        return geometry;
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        JsonSerializerOptions options =
            new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.WriteIndented = true;
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
