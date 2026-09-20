namespace ChemSculptor.Compute;

/// <summary>
/// 文件系统工作区管理器。
/// 负责确定和创建几何资产、计算作业的目录结构。
/// </summary>
public sealed class WorkspaceManager : ICalculationWorkspace
{
    private readonly CalculationWorkspaceOptions _options;

    /// <summary>创建工作区管理器。</summary>
    public WorkspaceManager(CalculationWorkspaceOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        if (string.IsNullOrWhiteSpace(options.RootDirectory))
        {
            throw new ArgumentException("工作区根目录不能为空。", nameof(options));
        }

        _options = options;
    }

    /// <summary>确保几何资产目录及其子目录存在。</summary>
    public Task EnsureGeometryWorkspaceAsync(
        string geometryId,
        CancellationToken cancellationToken = default)
    {
        string directory = GetGeometryDirectory(geometryId);
        Directory.CreateDirectory(directory);
        return Task.CompletedTask;
    }

    /// <summary>确保计算作业目录及其子目录存在。</summary>
    public Task EnsureJobWorkspaceAsync(
        string jobId,
        CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(GetInputDirectory(jobId));
        Directory.CreateDirectory(GetRunDirectory(jobId));
        Directory.CreateDirectory(GetResultDirectory(jobId));
        return Task.CompletedTask;
    }

    /// <summary>获取几何资产目录。</summary>
    public string GetGeometryDirectory(string geometryId)
    {
        string safeGeometryId = ValidateIdentifier(geometryId, nameof(geometryId));
        return Path.Combine(
            _options.RootDirectory,
            _options.GeometriesDirectoryName,
            safeGeometryId);
    }

    /// <summary>获取作业目录。</summary>
    public string GetJobDirectory(string jobId)
    {
        string safeJobId = ValidateIdentifier(jobId, nameof(jobId));
        return Path.Combine(
            _options.RootDirectory,
            _options.JobsDirectoryName,
            safeJobId);
    }

    /// <summary>获取输入目录。</summary>
    public string GetInputDirectory(string jobId)
    {
        return Path.Combine(GetJobDirectory(jobId), _options.InputDirectoryName);
    }

    /// <summary>获取运行目录。</summary>
    public string GetRunDirectory(string jobId)
    {
        return Path.Combine(GetJobDirectory(jobId), _options.RunDirectoryName);
    }

    /// <summary>获取结果目录。</summary>
    public string GetResultDirectory(string jobId)
    {
        return Path.Combine(GetJobDirectory(jobId), _options.ResultsDirectoryName);
    }

    /// <summary>获取作业清单文件路径。</summary>
    public string GetJobManifestPath(string jobId)
    {
        return Path.Combine(GetJobDirectory(jobId), CalculationWorkspacePaths.JobManifestFileName);
    }

    /// <summary>获取作业坐标文件路径。</summary>
    public string GetJobCoordinatesPath(string jobId)
    {
        return Path.Combine(
            GetInputDirectory(jobId),
            CalculationWorkspacePaths.JobCoordinatesFileName);
    }

    /// <summary>获取作业结果文件路径。</summary>
    public string GetJobResultPath(string jobId)
    {
        return Path.Combine(
            GetResultDirectory(jobId),
            CalculationWorkspacePaths.JobResultFileName);
    }

    /// <summary>获取作业输出文件路径。</summary>
    public string GetJobOutputPath(string jobId)
    {
        return Path.Combine(
            GetRunDirectory(jobId),
            CalculationWorkspacePaths.JobOutputFileName);
    }

    /// <summary>
    /// 校验标识是否可用于目录名称。
    /// 只允许字母、数字、连字符和下划线，防止路径穿越。
    /// </summary>
    private static string ValidateIdentifier(string identifier, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(identifier))
        {
            throw new ArgumentException("标识不能为空。", parameterName);
        }

        for (int index = 0; index < identifier.Length; index++)
        {
            char character = identifier[index];
            bool allowed = char.IsLetterOrDigit(character)
                || character == '-'
                || character == '_';

            if (!allowed)
            {
                throw new ArgumentException(
                    "标识只能包含字母、数字、连字符和下划线。",
                    parameterName);
            }
        }

        return identifier;
    }
}
