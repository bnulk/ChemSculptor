using ChemSculptor.Compute;
using ChemSculptor.InputProcessor.GeometryIntake;
using System.Diagnostics;

namespace ChemSculptor.Compute.Gaussian;

/// <summary>
/// Gaussian 16 程序适配器。
/// 负责把通用计算方案转换为 Gaussian 16 的输入文件、命令行参数和运行上下文。
/// </summary>
public sealed class Gaussian16ProgramAdapter : IQuantumProgramAdapter
{
    /// <summary>默认程序名称。</summary>
    public const string ProgramNameValue = "Gaussian 16";

    /// <summary>当前阶段的默认内存设置。</summary>
    public const string DefaultMemory = "4GB";

    private readonly GaussianInputWriter _inputWriter;
    private readonly GaussianOutputParser _outputParser;
    private readonly GaussianResultTranslator _resultTranslator;
    private readonly GaussianProcessingPlanTranslator _processingPlanTranslator;
    private readonly Gaussian16ProgramAdapterOptions _options;

    /// <summary>创建 Gaussian 16 适配器。</summary>
    public Gaussian16ProgramAdapter(
        GaussianInputWriter inputWriter,
        GaussianOutputParser outputParser,
        GaussianResultTranslator resultTranslator,
        GaussianProcessingPlanTranslator processingPlanTranslator,
        Gaussian16ProgramAdapterOptions options)
    {
        if (inputWriter == null)
        {
            throw new ArgumentNullException(nameof(inputWriter));
        }

        if (outputParser == null)
        {
            throw new ArgumentNullException(nameof(outputParser));
        }

        if (resultTranslator == null)
        {
            throw new ArgumentNullException(nameof(resultTranslator));
        }

        if (processingPlanTranslator == null)
        {
            throw new ArgumentNullException(nameof(processingPlanTranslator));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _inputWriter = inputWriter;
        _outputParser = outputParser;
        _resultTranslator = resultTranslator;
        _processingPlanTranslator = processingPlanTranslator;
        _options = options;
    }

    /// <summary>程序名称。</summary>
    public string ProgramName
    {
        get { return ProgramNameValue; }
    }

    /// <summary>判断计算方案是否指向 Gaussian 16。</summary>
    public bool CanRun(CalculationSpec spec)
    {
        if (spec == null)
        {
            return false;
        }

        return string.Equals(
            spec.Program,
            ProgramNameValue,
            StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>生成 Gaussian 16 输入文件。</summary>
    public Task WriteInputAsync(
        CalculationSpec spec,
        CanonicalGeometry geometry,
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        if (!CanRun(spec))
        {
            throw new NotSupportedException("当前适配器只能处理 Gaussian 16 计算方案。");
        }

        GaussianInputOptions options = new GaussianInputOptions();
        options.Memory = _options.Memory;
        options.ProcessorCount = _options.ProcessorCount;
        options.CheckpointFilePath = Path.GetFileName(Path.ChangeExtension(outputPath, ".chk"));
        options.Title = "ChemSculptor single point calculation";

        return _inputWriter.WriteAsync(
            spec,
            geometry,
            options,
            outputPath,
            cancellationToken);
    }

    /// <summary>获取 Gaussian 输入文件名称。</summary>
    public string GetInputFileName(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            throw new ArgumentException("作业标识不能为空。", nameof(jobId));
        }

        return jobId + ".gjf";
    }

    /// <summary>获取 Gaussian 允许下载的文件规则。</summary>
    public IReadOnlyList<CalculationArtifactPattern> GetArtifactPatterns()
    {
        List<CalculationArtifactPattern> patterns =
            new List<CalculationArtifactPattern>();
        patterns.Add(CreateArtifactPattern(
            "output.log",
            CalculationArtifactKind.PrimaryOutput,
            "text/plain; charset=utf-8",
            false));
        patterns.Add(CreateArtifactPattern(
            "*.gjf",
            CalculationArtifactKind.Input,
            "text/plain; charset=utf-8",
            false));
        patterns.Add(CreateArtifactPattern(
            "*.fchk",
            CalculationArtifactKind.RestartState,
            "application/octet-stream",
            true));
        patterns.Add(CreateArtifactPattern(
            "*.chk",
            CalculationArtifactKind.RestartState,
            "application/octet-stream",
            true));
        patterns.Add(CreateArtifactPattern(
            "stdout.log",
            CalculationArtifactKind.SupportingOutput,
            "text/plain; charset=utf-8",
            false));
        patterns.Add(CreateArtifactPattern(
            "stderr.log",
            CalculationArtifactKind.SupportingOutput,
            "text/plain; charset=utf-8",
            false));
        return patterns;
    }

    /// <summary>检查检查点文件并调用 formchk 生成 fchk。</summary>
    public async Task PostProcessAsync(
        CalculationJob job,
        CancellationToken cancellationToken = default)
    {
        if (job == null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (string.IsNullOrWhiteSpace(job.RunDirectory)
            || !Directory.Exists(job.RunDirectory))
        {
            return;
        }

        string checkpointPath = FindLatestCheckpoint(job.RunDirectory);
        if (checkpointPath.Length == 0)
        {
            return;
        }

        string formattedCheckpointPath =
            Path.ChangeExtension(checkpointPath, ".fchk");

        ProcessStartInfo startInfo = new ProcessStartInfo();
        startInfo.FileName = _options.FormChkExecutablePath;
        startInfo.WorkingDirectory = job.RunDirectory;
        startInfo.UseShellExecute = false;
        startInfo.RedirectStandardOutput = true;
        startInfo.RedirectStandardError = true;
        startInfo.CreateNoWindow = true;
        startInfo.ArgumentList.Add(Path.GetFileName(checkpointPath));
        startInfo.ArgumentList.Add(Path.GetFileName(formattedCheckpointPath));

        using (Process process = new Process())
        {
            process.StartInfo = startInfo;
            process.Start();

            Task<string> standardOutputTask =
                process.StandardOutput.ReadToEndAsync(cancellationToken);
            Task<string> standardErrorTask =
                process.StandardError.ReadToEndAsync(cancellationToken);

            await process.WaitForExitAsync(cancellationToken);

            string standardOutput = await standardOutputTask;
            string standardError = await standardErrorTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    "formchk 失败，退出码 " +
                    process.ExitCode.ToString() +
                    "。标准错误：" +
                    standardError);
            }

            if (!File.Exists(formattedCheckpointPath))
            {
                throw new InvalidOperationException(
                    "formchk 已结束，但没有生成 fchk 文件：" +
                    formattedCheckpointPath);
            }
        }
    }

    /// <summary>
    /// 构建 Gaussian 16 的本机执行上下文。
    /// 默认调用 PATH 中的 g16，并把输入文件作为第一个参数、输出文件作为第二个参数。
    /// </summary>
    public CalculationExecutionContext BuildExecutionContext(
        CalculationJob job,
        CalculationSpec spec)
    {
        if (job == null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        if (!CanRun(spec))
        {
            throw new NotSupportedException("当前适配器只能处理 Gaussian 16 计算方案。");
        }

        if (string.IsNullOrWhiteSpace(job.RunDirectory))
        {
            throw new ArgumentException("运行目录不能为空。", nameof(job));
        }

        if (string.IsNullOrWhiteSpace(job.InputFilePath))
        {
            throw new ArgumentException("输入文件路径不能为空。", nameof(job));
        }

        if (string.IsNullOrWhiteSpace(job.OutputFilePath))
        {
            throw new ArgumentException("输出文件路径不能为空。", nameof(job));
        }

        CalculationExecutionContext context = new CalculationExecutionContext();
        context.JobId = job.JobId;
        context.WorkspaceDirectory = job.WorkspaceDirectory;
        context.RunDirectory = job.RunDirectory;
        context.InputFilePath = job.InputFilePath;
        context.OutputFilePath = job.OutputFilePath;
        context.ExecutablePath = _options.ExecutablePath;
        context.ProcessorCount = _options.ProcessorCount;
        context.Memory = _options.Memory;
        context.CheckpointFilePath = Path.ChangeExtension(job.InputFilePath, ".chk");
        context.Arguments.Add(job.InputFilePath);
        context.Arguments.Add(job.OutputFilePath);

        if (!string.IsNullOrWhiteSpace(_options.ExecutableDirectory))
        {
            context.EnvironmentVariables["GAUSS_EXEDIR"] = _options.ExecutableDirectory;
        }

        return context;
    }

    /// <summary>解析 Gaussian 输出文件。</summary>
    public async Task<CalculationResult> ParseOutputAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        GaussianOutput gaussianOutput =
            await _outputParser.ParseAsync(outputPath, cancellationToken);

        return _resultTranslator.Translate(gaussianOutput);
    }

    /// <summary>把通用处理方案翻译为 Gaussian 专用方案。</summary>
    public Task<ProgramProcessingPlan> TranslateProcessingPlanAsync(
        CalculationJob job,
        CalculationProcessingPlan processingPlan,
        CancellationToken cancellationToken = default)
    {
        if (job == null)
        {
            throw new ArgumentNullException(nameof(job));
        }

        GaussianProcessingPlan gaussianPlan =
            _processingPlanTranslator.Translate(processingPlan);

        gaussianPlan.JobId = job.JobId;
        ProgramProcessingPlan programPlan =
            _processingPlanTranslator.ToProgramPlan(gaussianPlan);

        return Task.FromResult(programPlan);
    }

    private static string FindLatestCheckpoint(string runDirectory)
    {
        string[] checkpointFiles =
            Directory.GetFiles(runDirectory, "*.chk", SearchOption.TopDirectoryOnly);

        if (checkpointFiles.Length == 0)
        {
            return string.Empty;
        }

        string latestPath = checkpointFiles[0];
        DateTime latestWriteTime = File.GetLastWriteTimeUtc(latestPath);

        for (int index = 1; index < checkpointFiles.Length; index++)
        {
            DateTime writeTime =
                File.GetLastWriteTimeUtc(checkpointFiles[index]);

            if (writeTime > latestWriteTime)
            {
                latestPath = checkpointFiles[index];
                latestWriteTime = writeTime;
            }
        }

        return latestPath;
    }

    private static CalculationArtifactPattern CreateArtifactPattern(
        string filePattern,
        CalculationArtifactKind kind,
        string mediaType,
        bool canUseForRestart)
    {
        CalculationArtifactPattern pattern = new CalculationArtifactPattern();
        pattern.FilePattern = filePattern;
        pattern.Kind = kind;
        pattern.MediaType = mediaType;
        pattern.CanUseForRestart = canUseForRestart;
        return pattern;
    }
}
