using ChemSculptor.Compute;
using ChemSculptor.InputProcessor.GeometryIntake;

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
    private readonly Gaussian16ProgramAdapterOptions _options;

    /// <summary>创建 Gaussian 16 适配器。</summary>
    public Gaussian16ProgramAdapter(
        GaussianInputWriter inputWriter,
        Gaussian16ProgramAdapterOptions options)
    {
        if (inputWriter == null)
        {
            throw new ArgumentNullException(nameof(inputWriter));
        }

        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        _inputWriter = inputWriter;
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

    /// <summary>解析 Gaussian 输出文件；当前阶段尚未实现。</summary>
    public Task<CalculationResult> ParseOutputAsync(
        string outputPath,
        CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Gaussian 输出解析将在后续阶段实现。");
    }
}
