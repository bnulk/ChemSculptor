using ChemSculptor.Anomaly.Checks;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Anomaly.Registry;
using ChemSculptor.Anomaly.Storage;
using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Compute.Gaussian.Anomaly.Recovery;
using ChemSculptor.Domain;
using ChemSculptor.InputProcessor.GeometryIntake;
using ChemSculptor.Skills.Common;
using ChemSculptor.Skills.Common.OptimizationRecovery;
using ChemSculptor.Skills.Gaussian.Anomaly.Recovery;

namespace ChemSculptor.Core.Tests;

/// <summary>优化恢复链测试。</summary>
public class OptimizationRecoveryWorkflowTests
{
    /// <summary>
    /// 从未收敛异常生成增加 maxcycles 的修正方案，
    /// 创建新 opt 输入并执行派生优化作业。
    /// </summary>
    [Fact]
    public async Task CreatesAndExecutesOptimizationRecovery()
    {
        string root = CreateTemporaryRoot();

        try
        {
            CalculationWorkspaceOptions workspaceOptions =
                new CalculationWorkspaceOptions();
            workspaceOptions.RootDirectory = root;
            WorkspaceManager workspace =
                new WorkspaceManager(workspaceOptions);
            FileCalculationRepository calculationRepository =
                new FileCalculationRepository(workspace);
            FileAnomalyRepository anomalyRepository =
                new FileAnomalyRepository(workspace);

            CalculationJob sourceJob =
                await CreateSourceOptimizationJobAsync(
                    workspace,
                    calculationRepository);
            AnomalyRecord anomalyRecord =
                await CreateAnomalyRecordAsync(
                    sourceJob,
                    anomalyRepository);
            AnomalyCheckResult anomalyCheck =
                CreateNotConvergedCheck(
                    sourceJob,
                    anomalyRecord);

            OptimizationFailureDiagnosisSkill diagnosisSkill =
                new OptimizationFailureDiagnosisSkill();
            OptimizationFailureDiagnosisResult diagnosis =
                await ExecuteJsonSkillAsync<
                    OptimizationFailureDiagnosisResult>(
                        diagnosisSkill,
                        "anomaly",
                        anomalyCheck);
            Assert.Single(diagnosis.Diagnoses);
            Assert.Equal(
                OptimizationAnomalyCodes.NotConverged,
                diagnosis.Diagnoses[0].AnomalyCode);

            OptimizationCorrectionPlanningSkill planningSkill =
                new OptimizationCorrectionPlanningSkill(
                    calculationRepository,
                    anomalyRepository);
            OptimizationCorrectionPlanningResult planning =
                await ExecuteJsonSkillAsync<
                    OptimizationCorrectionPlanningResult>(
                        planningSkill,
                        "diagnosis",
                        diagnosis);
            Assert.True(planning.PlanCreated);
            Assert.NotNull(planning.Plan);
            Assert.Equal(
                CorrectionIntentCodes
                    .IncreaseOptimizationMaxCycles,
                planning.Plan.Option!.IntentCode);

            FixedQuantumProgramAdapter adapter =
                new FixedQuantumProgramAdapter();
            QuantumProgramAdapterRegistry adapterRegistry =
                new QuantumProgramAdapterRegistry(
                    new IQuantumProgramAdapter[] { adapter });
            GaussianRecoveryJobProvider provider =
                new GaussianRecoveryJobProvider(
                    workspace,
                    calculationRepository,
                    adapterRegistry,
                    new GaussianRecoveryInputWriter(
                        new GaussianRouteKeywordBuilder()));
            AnomalyProviderRegistry anomalyRegistry =
                new AnomalyProviderRegistry();
            anomalyRegistry.RegisterRecoveryJobProvider(provider);

            OptimizationRecoveryJobCreationSkill creationSkill =
                new OptimizationRecoveryJobCreationSkill(
                    anomalyRegistry,
                    calculationRepository,
                    anomalyRepository);
            RecoveryJobPreparationResult preparation =
                await ExecuteJsonSkillAsync<
                    RecoveryJobPreparationResult>(
                        creationSkill,
                        "correctionPlan",
                        planning);

            Assert.True(preparation.Succeeded);
            Assert.NotNull(preparation.RecoveryJob);
            CalculationJob recoveryJob = preparation.RecoveryJob;
            Assert.Equal(
                sourceJob.JobId,
                recoveryJob.ParentJobId);
            Assert.Equal(
                sourceJob.AttemptNumber + 1,
                recoveryJob.AttemptNumber);
            Assert.Equal(
                "200",
                recoveryJob.Spec.ExtraOptions[
                    CalculationOptionKeys.OptimizationMaxCycles]);

            string recoveryInput =
                await File.ReadAllTextAsync(
                    recoveryJob.InputFilePath);
            Assert.Contains("opt=MaxCycles=200", recoveryInput);
            Assert.DoesNotContain(
                "opt=MaxCycles=100",
                recoveryInput);

            ImmediateComputeBackend backend =
                new ImmediateComputeBackend();
            OptimizationRecoveryJobExecutionSkill executionSkill =
                new OptimizationRecoveryJobExecutionSkill(
                    backend,
                    adapterRegistry,
                    calculationRepository,
                    anomalyRepository);
            OptimizationRecoveryExecutionResult execution =
                await ExecuteJsonSkillAsync<
                    OptimizationRecoveryExecutionResult>(
                        executionSkill,
                        "correctionPlan",
                        planning,
                        "recoveryJob",
                        preparation);

            Assert.True(execution.Attempted);
            Assert.True(execution.Succeeded);
            Assert.NotNull(execution.Result);
            File.WriteAllText(
                recoveryJob.OutputFilePath,
                "Normal termination");

            OptimizationRecoveryValidationSkill validationSkill =
                new OptimizationRecoveryValidationSkill(
                    anomalyRepository,
                    new OptimizationConvergenceCheck());
            RecoveryValidationResult validation =
                await ExecuteJsonSkillAsync<
                    RecoveryValidationResult>(
                        validationSkill,
                        "originalAnomaly",
                        anomalyCheck,
                        "recoveryExecution",
                        execution);

            Assert.True(validation.Succeeded);
            Assert.True(validation.ResolvedOriginalAnomaly);

            AnomalyRecord? savedRecord =
                await anomalyRepository.GetAsync(
                    sourceJob.JobId,
                    anomalyRecord.Id);
            Assert.NotNull(savedRecord);
            Assert.Equal(
                AnomalyRecordStatus.Resolved,
                savedRecord.Status);
            Assert.Single(savedRecord.RecoveryAttempts);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static async Task<CalculationJob>
        CreateSourceOptimizationJobAsync(
            WorkspaceManager workspace,
            FileCalculationRepository repository)
    {
        string jobId = "job-source-optimization";
        await workspace.EnsureJobWorkspaceAsync(jobId);
        string inputPath = Path.Combine(
            workspace.GetRunDirectory(jobId),
            jobId + ".gjf");
        string outputPath =
            workspace.GetJobOutputPath(jobId);

        await File.WriteAllTextAsync(
            inputPath,
            CreateSourceInputText());

        CalculationSpec spec =
            CalculationDefaults.CreateDefaultOptimization();
        spec.ExtraOptions[
            CalculationOptionKeys.OptimizationMaxCycles] = "100";

        CalculationJob job = new CalculationJob();
        job.JobId = jobId;
        job.TaskId = "task-optimization";
        job.SessionId = "session-optimization";
        job.WorkflowId = "workflow-optimization";
        job.GeometryId = "geometry-optimization";
        job.Goal = "优化恢复测试";
        job.Spec = spec;
        job.State = CalculationJobState.Failed;
        job.WorkspaceDirectory =
            workspace.GetJobDirectory(jobId);
        job.RunDirectory =
            workspace.GetRunDirectory(jobId);
        job.SourceInputFilePath = inputPath;
        job.InputFilePath = inputPath;
        job.OutputFilePath = outputPath;

        await repository.SaveJobAsync(job);
        return job;
    }

    private static async Task<AnomalyRecord>
        CreateAnomalyRecordAsync(
            CalculationJob sourceJob,
            FileAnomalyRepository repository)
    {
        AnomalyRecord record = new AnomalyRecord();
        record.Id = "anomaly-optimization-recovery";
        record.JobId = sourceJob.JobId;
        record.WorkflowId = sourceJob.WorkflowId;
        await repository.SaveAsync(record);
        return record;
    }

    private static AnomalyCheckResult CreateNotConvergedCheck(
        CalculationJob sourceJob,
        AnomalyRecord record)
    {
        AnomalyCheckResult check = new AnomalyCheckResult();
        check.Code =
            CommonAnomalyCheckCodes.OptimizationConvergence;
        check.JobId = sourceJob.JobId;
        check.AnomalyRecordId = record.Id;
        check.Status = AnomalyCheckStatus.Finding;
        check.Summary = "发现优化未收敛。";

        AnomalyFinding finding = new AnomalyFinding();
        finding.Code = OptimizationAnomalyCodes.NotConverged;
        finding.Title = "几何优化未收敛";
        finding.Category = AnomalyCategory.Scientific;
        finding.Severity = AnomalySeverity.Error;
        finding.Confidence = 1.0;
        finding.IsBlocking = true;
        finding.Evidence.Add(CreateEvidence(
            "optimization.not_converged",
            "最终优化没有收敛。"));
        check.Findings.Add(finding);
        return check;
    }

    private static AnomalyEvidence CreateEvidence(
        string code,
        string message)
    {
        AnomalyEvidence evidence = new AnomalyEvidence();
        evidence.Code = code;
        evidence.Message = message;
        evidence.Source = "test";
        return evidence;
    }

    private static string CreateSourceInputText()
    {
        return
            "%chk=source.chk\n" +
            "%mem=4GB\n" +
            "%nprocshared=4\n\n" +
            "#p CAM-B3LYP/6-31G* opt=MaxCycles=100 em=GD3BJ\n\n" +
            "optimization recovery test\n\n" +
            "0 1\n" +
            "O 0.000000 0.000000 0.000000\n" +
            "H 0.000000 0.700000 -0.400000\n\n";
    }

    private static async Task<T> ExecuteJsonSkillAsync<T>(
        ISkill skill,
        string inputName,
        object inputValue)
        where T : class
    {
        Dictionary<string, string> inputs =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
        inputs[inputName] = SkillJson.Serialize(inputValue);
        return await ExecuteJsonSkillAsync<T>(skill, inputs);
    }

    private static async Task<T> ExecuteJsonSkillAsync<T>(
        ISkill skill,
        string firstName,
        object firstValue,
        string secondName,
        object secondValue)
        where T : class
    {
        Dictionary<string, string> inputs =
            new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
        inputs[firstName] = SkillJson.Serialize(firstValue);
        inputs[secondName] = SkillJson.Serialize(secondValue);
        return await ExecuteJsonSkillAsync<T>(skill, inputs);
    }

    private static async Task<T> ExecuteJsonSkillAsync<T>(
        ISkill skill,
        Dictionary<string, string> inputs)
        where T : class
    {
        TaskRequest request = new TaskRequest();
        request.WorkflowId = "workflow-optimization";
        request.NodeId = "node";
        request.SkillId = skill.Name;
        request.Inputs = inputs;
        TaskResult taskResult =
            await skill.ExecuteAsync(request);
        Assert.True(taskResult.Succeeded);
        return SkillJson.Deserialize<T>(taskResult.Output!);
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

    private sealed class ImmediateComputeBackend : IComputeBackend
    {
        public string Name
        {
            get { return "test-backend"; }
        }

        public Task<string> SubmitAsync(
            CalculationJob job,
            CalculationExecutionContext context,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(job.JobId);
        }

        public Task<CalculationJobState> GetStatusAsync(
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                CalculationJobState.Completed);
        }

        public Task CancelAsync(
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }

    private sealed class FixedQuantumProgramAdapter
        : IQuantumProgramAdapter, IOptimizationOutputParser
    {
        public string ProgramName
        {
            get { return Gaussian16ProgramAdapter.ProgramNameValue; }
        }

        public bool CanRun(CalculationSpec spec)
        {
            return true;
        }

        public Task WriteInputAsync(
            CalculationSpec spec,
            CanonicalGeometry geometry,
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public string GetInputFileName(string jobId)
        {
            return jobId + ".gjf";
        }

        public IReadOnlyList<CalculationArtifactPattern>
            GetArtifactPatterns(
                CalculationArtifactDiscoveryContext context)
        {
            return new List<CalculationArtifactPattern>();
        }

        public IReadOnlyList<ProgramParameterTranslation>
            GetParameterTranslations(CalculationSpec spec)
        {
            return new List<ProgramParameterTranslation>();
        }

        public Task PostProcessAsync(
            CalculationJob job,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public CalculationExecutionContext BuildExecutionContext(
            CalculationJob job,
            CalculationSpec spec)
        {
            CalculationExecutionContext context =
                new CalculationExecutionContext();
            context.JobId = job.JobId;
            context.RunDirectory = job.RunDirectory;
            context.InputFilePath = job.InputFilePath;
            context.OutputFilePath = job.OutputFilePath;
            return context;
        }

        public Task<CalculationResult> ParseOutputAsync(
            string outputPath,
            CancellationToken cancellationToken = default)
        {
            CalculationResult result = new CalculationResult();
            result.NormalTermination = true;
            result.OutputFilePath = outputPath;
            return Task.FromResult(result);
        }

        public Task<ProgramProcessingPlan>
            TranslateProcessingPlanAsync(
                CalculationJob job,
                CalculationProcessingPlan processingPlan,
                CancellationToken cancellationToken = default)
        {
            ProgramProcessingPlan plan =
                new ProgramProcessingPlan();
            plan.JobId = job.JobId;
            plan.Program = ProgramName;
            return Task.FromResult(plan);
        }

        public Task<OptimizationResult>
            ParseOptimizationOutputAsync(
                string outputPath,
                CancellationToken cancellationToken = default)
        {
            OptimizationResult result =
                CreateSuccessfulOptimizationResult();
            result.OutputFilePath = outputPath;
            return Task.FromResult(result);
        }
    }

    private static OptimizationResult
        CreateSuccessfulOptimizationResult()
    {
        OptimizationResult result =
            new OptimizationResult();
        result.JobId = "job-recovery";
        result.Program =
            Gaussian16ProgramAdapter.ProgramNameValue;
        result.NormalTermination = true;
        result.OptimizationConverged = true;
        result.StepCount = 1;
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
        convergence.Converged = true;
        convergence.MaximumForceConverged = true;
        convergence.RmsForceConverged = true;
        convergence.MaximumDisplacementConverged = true;
        convergence.RmsDisplacementConverged = true;
        convergence.MaximumForce = 0.000100;
        convergence.MaximumForceThreshold = 0.000450;
        convergence.RmsForce = 0.000040;
        convergence.RmsForceThreshold = 0.000300;
        convergence.MaximumDisplacement = 0.000100;
        convergence.MaximumDisplacementThreshold = 0.001800;
        convergence.RmsDisplacement = 0.000050;
        convergence.RmsDisplacementThreshold = 0.001200;
        result.FinalConvergence = convergence;

        OptimizationStep step = new OptimizationStep();
        step.StepIndex = 1;
        step.Geometry = geometry;
        step.Energy = -76.200000;
        step.ScfConverged = true;
        step.ScfIterations = 5;
        step.Convergence = convergence;
        result.Trajectory.Add(step);
        return result;
    }
}
