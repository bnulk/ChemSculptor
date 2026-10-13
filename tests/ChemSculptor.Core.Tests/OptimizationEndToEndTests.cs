using ChemSculptor.Anomaly.Checks;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Conversation;
using ChemSculptor.Domain;
using ChemSculptor.InputProcessor.GeometryIntake;
using ChemSculptor.ScientificData.Extraction;
using ChemSculptor.ScientificData.Storage;
using ChemSculptor.Skills.Common;

namespace ChemSculptor.Core.Tests;

/// <summary>优化端到端基础链测试。</summary>
public class OptimizationEndToEndTests
{
    /// <summary>
    /// 验证 route 输入、opt 输入生成、轨迹解析、异常识别和科学数据保存。
    /// </summary>
    [Fact]
    public async Task RunsOptimizationInputToScienceData()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationSpec spec =
                await ResolveOptimizationSpecAsync();
            Assert.Equal(
                CalculationTaskType.Optimization,
                spec.TaskType);

            string inputPath = Path.Combine(
                root,
                "optimization.gjf");
            GaussianInputOptions options =
                new GaussianInputOptions();
            options.Memory = "4GB";
            options.ProcessorCount = 4;
            options.Title = "optimization end to end";

            GaussianInputWriter writer =
                new GaussianInputWriter();
            await writer.WriteAsync(
                spec,
                CreateWaterGeometry(),
                options,
                inputPath);
            string inputText =
                await File.ReadAllTextAsync(inputPath);

            Assert.Contains(" opt", inputText);
            Assert.DoesNotContain(" SP", inputText);
            Assert.Contains("em=GD3BJ", inputText);
            Assert.Contains("scfcyc=200", inputText);
            Assert.Contains("nosymm", inputText);

            string outputPath = Path.Combine(
                root,
                "optimization.log");
            await File.WriteAllTextAsync(
                outputPath,
                CreateNonConvergedOptimizationOutput());

            GaussianOutputParser parser =
                new GaussianOutputParser();
            GaussianOutput gaussianOutput =
                await parser.ParseAsync(outputPath);
            GaussianResultTranslator translator =
                new GaussianResultTranslator();
            OptimizationResult optimizationResult =
                translator.TranslateOptimization(
                    gaussianOutput);

            Assert.Equal(2, optimizationResult.StepCount);
            Assert.Equal(2, optimizationResult.Trajectory.Count);
            Assert.False(
                optimizationResult.OptimizationConverged);
            Assert.True(
                optimizationResult.Trajectory[0].ScfConverged);
            Assert.True(
                optimizationResult.Trajectory[1].ScfConverged);

            CalculationJob job = new CalculationJob();
            job.JobId = "job-end-to-end";
            job.Spec = spec;

            AnomalyContext anomalyContext =
                new AnomalyContext();
            anomalyContext.Job = job;
            anomalyContext.OptimizationResult =
                optimizationResult;
            AnomalyCheckResult anomaly =
                await new OptimizationConvergenceCheck()
                    .CheckAsync(anomalyContext);

            Assert.Equal(
                AnomalyCheckStatus.Finding,
                anomaly.Status);
            Assert.Contains(
                anomaly.Findings,
                finding =>
                    finding.Code
                        == OptimizationAnomalyCodes
                            .NotConverged);

            ScientificDataRepositoryOptions repositoryOptions =
                new ScientificDataRepositoryOptions();
            repositoryOptions.RootDirectory = Path.Combine(
                root,
                "scientific-data");
            FileScientificDataRepository repository =
                new FileScientificDataRepository(
                    repositoryOptions);
            OptimizationScientificDataRecordSkill recordSkill =
                new OptimizationScientificDataRecordSkill(
                    repository);

            OptimizationCalculationSkillResult optimization =
                new OptimizationCalculationSkillResult();
            optimization.Job = job;
            optimization.Result = optimizationResult;

            CalculationWorkflowProcessingPlanSkillResult plan =
                new CalculationWorkflowProcessingPlanSkillResult();
            plan.Plan.JobId = job.JobId;
            plan.Plan.Outcome =
                CalculationProcessingOutcome.NeedsUserInput;
            plan.Plan.Summary =
                "优化未收敛，需要进入恢复工作流。";

            TaskRequest recordRequest = new TaskRequest();
            recordRequest.WorkflowId = "optimization-end-to-end";
            recordRequest.NodeId = "optimization-science-data";
            recordRequest.SkillId = recordSkill.Name;
            recordRequest.Inputs["optimization"] =
                SkillJson.Serialize(optimization);
            recordRequest.Inputs["processingPlan"] =
                SkillJson.Serialize(plan);

            TaskResult recordResult =
                await recordSkill.ExecuteAsync(recordRequest);

            Assert.True(recordResult.Succeeded);
            Assert.Single(repository.List());
            Assert.Equal(
                "optimization-job-end-to-end",
                repository.List()[0].Id);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static async Task<CalculationSpec>
        ResolveOptimizationSpecAsync()
    {
        string text =
            "#p cam-b3lyp/6-31G* opt em=GD3BJ scfcyc=200 nosymm";
        InMemoryConversationRepository repository =
            new InMemoryConversationRepository();
        CalculationParameterConversationService service =
            new CalculationParameterConversationService(
                repository,
                new RuleBasedCalculationParameterExtractor(),
                new CalculationParameterResolver());
        CalculationParameterResolutionContext context =
            new CalculationParameterResolutionContext();
        context.Engine = CalculationDefaults.DefaultProgram;
        context.TaskType = CalculationTaskType.Optimization;
        context.ElectronCount = 10;

        ParameterConversationTurn turn =
            await service.HandleAsync(
                "session-end-to-end",
                text,
                "O 0.0 0.0 0.0",
                CalculationDefaults.CreateDefaultOptimization(),
                context);
        Assert.Equal(
            ParameterConversationTurnStatus.ReadyToSubmit,
            turn.Status);
        return turn.Resolution!.Spec;
    }

    private static CanonicalGeometry CreateWaterGeometry()
    {
        CanonicalGeometry geometry =
            new CanonicalGeometry();
        geometry.Units = GeometryUnits.Angstrom;
        geometry.Atoms.Add(CreateAtom(
            1,
            "O",
            0.0,
            0.0,
            0.1173));
        geometry.Atoms.Add(CreateAtom(
            2,
            "H",
            0.0,
            0.7572,
            -0.4692));
        geometry.Atoms.Add(CreateAtom(
            3,
            "H",
            0.0,
            -0.7572,
            -0.4692));
        return geometry;
    }

    private static CanonicalAtom CreateAtom(
        int index,
        string element,
        double x,
        double y,
        double z)
    {
        CanonicalAtom atom = new CanonicalAtom();
        atom.Index = index;
        atom.Element = element;
        atom.X = x;
        atom.Y = y;
        atom.Z = z;
        return atom;
    }

    private static string CreateNonConvergedOptimizationOutput()
    {
        return
            " Step number   1  out of a maximum of  100\n" +
            CreateGeometryBlock(
                "0.000000",
                "0.000000",
                "0.100000") +
            " SCF Done:  E(RB3LYP) =  -76.100000     A.U. after    7 cycles\n" +
            CreateConvergenceBlock("NO") +
            " Step number   2  out of a maximum of  100\n" +
            CreateGeometryBlock(
                "0.000500",
                "0.000000",
                "0.100000") +
            " SCF Done:  E(RB3LYP) =  -76.200000     A.U. after    5 cycles\n" +
            CreateConvergenceBlock("NO") +
            " Normal termination of Gaussian 16\n";
    }

    private static string CreateGeometryBlock(
        string oxygenX,
        string oxygenY,
        string oxygenZ)
    {
        return
            "                         Standard orientation:\n" +
            " ---------------------------------------------------------------------\n" +
            " Center     Atomic      Atomic             Coordinates (Angstroms)\n" +
            " Number     Number       Type             X           Y           Z\n" +
            " ---------------------------------------------------------------------\n" +
            "      1          8           0        " +
            oxygenX +
            "        " +
            oxygenY +
            "        " +
            oxygenZ +
            "\n" +
            "      2          1           0        0.000000        0.700000       -0.400000\n" +
            " ---------------------------------------------------------------------\n";
    }

    private static string CreateConvergenceBlock(
        string converged)
    {
        return
            " Maximum Force            0.010000     0.000450     " +
            converged +
            "\n" +
            " RMS     Force            0.005000     0.000300     " +
            converged +
            "\n" +
            " Maximum Displacement     0.020000     0.001800     " +
            converged +
            "\n" +
            " RMS     Displacement     0.010000     0.001200     " +
            converged +
            "\n";
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
