namespace ChemSculptor.Compute;

/// <summary>路径安全检查结果。</summary>
internal enum CalculationWorkflowArtifactPathProbeResult
{
    /// <summary>路径存在且可以安全读取。</summary>
    Safe,

    /// <summary>路径不存在。</summary>
    Missing,

    /// <summary>路径位于作业目录之外。</summary>
    OutsideJob,

    /// <summary>路径包含重解析点或符号链接。</summary>
    ReparsePoint,

    /// <summary>路径元数据无法读取。</summary>
    Unreadable
}

/// <summary>目录枚举得到的一个文件系统条目。</summary>
internal sealed class CalculationWorkflowArtifactPathEntry
{
    /// <summary>完整路径。</summary>
    public string FullPath { get; set; } = string.Empty;

    /// <summary>文件或目录名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>是否目录。</summary>
    public bool IsDirectory { get; set; }

    /// <summary>是否为重解析点或符号链接。</summary>
    public bool IsReparsePoint { get; set; }

    /// <summary>读取文件属性时的错误。</summary>
    public string AttributesError { get; set; } = string.Empty;
}

/// <summary>一个目录下安全文件的数量和字节数。</summary>
internal sealed class CalculationWorkflowArtifactDirectoryMeasurement
{
    /// <summary>安全文件数。</summary>
    public int FileCount { get; set; }

    /// <summary>安全文件总字节数。</summary>
    public long TotalBytes { get; set; }

    /// <summary>递归过程中遇到的重解析点。</summary>
    public List<string> ReparsePointPaths { get; set; } =
        new List<string>();

    /// <summary>读取目录时的错误。</summary>
    public string Error { get; set; } = string.Empty;
}

/// <summary>
/// 全过程产物发现的路径边界策略。
/// 所有持久化文件必须位于作业目录内，目录遍历不得跟随重解析点。
/// </summary>
internal static class CalculationWorkflowArtifactPathPolicy
{
    private static readonly StringComparison PathComparison =
        OperatingSystem.IsWindows()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

    /// <summary>尝试把候选路径解析为作业目录内的完整路径。</summary>
    public static bool TryResolveWithinJob(
        string jobDirectory,
        string candidatePath,
        out string fullPath)
    {
        fullPath = string.Empty;

        if (string.IsNullOrWhiteSpace(jobDirectory)
            || string.IsNullOrWhiteSpace(candidatePath))
        {
            return false;
        }

        try
        {
            string jobFullPath = NormalizeDirectory(jobDirectory);
            string candidateFullPath = Path.GetFullPath(candidatePath);

            if (!IsWithin(jobFullPath, candidateFullPath))
            {
                return false;
            }

            fullPath = candidateFullPath;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (NotSupportedException)
        {
            return false;
        }
        catch (PathTooLongException)
        {
            return false;
        }
    }

    /// <summary>检查路径及其各级父目录是否存在重解析点。</summary>
    public static CalculationWorkflowArtifactPathProbeResult Probe(
        string jobDirectory,
        string candidatePath,
        out string fullPath,
        out string message)
    {
        fullPath = string.Empty;
        message = string.Empty;

        if (!TryResolveWithinJob(
                jobDirectory,
                candidatePath,
                out fullPath))
        {
            message = "候选路径位于作业目录之外。";
            return CalculationWorkflowArtifactPathProbeResult.OutsideJob;
        }

        string current = NormalizeDirectory(jobDirectory);
        string[] segments = Path.GetRelativePath(
                current,
                fullPath)
            .Split(
                new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar },
                StringSplitOptions.RemoveEmptyEntries);

        try
        {
            if (!Directory.Exists(current))
            {
                return CalculationWorkflowArtifactPathProbeResult.Missing;
            }

            if (IsReparsePoint(current))
            {
                message = "作业目录本身是重解析点。";
                return CalculationWorkflowArtifactPathProbeResult.ReparsePoint;
            }

            for (int index = 0; index < segments.Length; index++)
            {
                current = Path.Combine(current, segments[index]);

                if (!File.Exists(current) && !Directory.Exists(current))
                {
                    return CalculationWorkflowArtifactPathProbeResult.Missing;
                }

                if (IsReparsePoint(current))
                {
                    message = "路径包含重解析点或符号链接。";
                    return CalculationWorkflowArtifactPathProbeResult.ReparsePoint;
                }
            }

            return CalculationWorkflowArtifactPathProbeResult.Safe;
        }
        catch (UnauthorizedAccessException exception)
        {
            message = exception.Message;
            return CalculationWorkflowArtifactPathProbeResult.Unreadable;
        }
        catch (IOException exception)
        {
            message = exception.Message;
            return CalculationWorkflowArtifactPathProbeResult.Unreadable;
        }
        catch (ArgumentException exception)
        {
            message = exception.Message;
            return CalculationWorkflowArtifactPathProbeResult.Unreadable;
        }
    }

    /// <summary>把完整路径转换为作业目录内的正斜杠逻辑路径。</summary>
    public static string GetLogicalPath(
        string jobDirectory,
        string fullPath)
    {
        string relativePath = Path.GetRelativePath(
            NormalizeDirectory(jobDirectory),
            Path.GetFullPath(fullPath));
        return relativePath.Replace(
            Path.DirectorySeparatorChar,
            '/');
    }

    /// <summary>安全枚举一个目录的直接子项，不递归跟随链接。</summary>
    public static bool TryEnumerate(
        string directory,
        out List<CalculationWorkflowArtifactPathEntry> entries,
        out string error)
    {
        entries = new List<CalculationWorkflowArtifactPathEntry>();
        error = string.Empty;

        try
        {
            foreach (string path in Directory.EnumerateFileSystemEntries(
                directory,
                "*",
                SearchOption.TopDirectoryOnly))
            {
                CalculationWorkflowArtifactPathEntry entry =
                    new CalculationWorkflowArtifactPathEntry();
                entry.FullPath = path;
                entry.Name = Path.GetFileName(path);

                try
                {
                    FileAttributes attributes = File.GetAttributes(path);
                    entry.IsDirectory = attributes.HasFlag(
                        FileAttributes.Directory);
                    entry.IsReparsePoint = attributes.HasFlag(
                        FileAttributes.ReparsePoint);
                }
                catch (Exception exception) when (
                    exception is IOException
                    || exception is UnauthorizedAccessException
                    || exception is ArgumentException)
                {
                    entry.AttributesError = exception.Message;
                }

                entries.Add(entry);
            }

            entries.Sort(
                delegate(
                    CalculationWorkflowArtifactPathEntry left,
                    CalculationWorkflowArtifactPathEntry right)
                {
                    return string.Compare(
                        left.FullPath,
                        right.FullPath,
                        StringComparison.OrdinalIgnoreCase);
                });
            return true;
        }
        catch (UnauthorizedAccessException exception)
        {
            error = exception.Message;
            return false;
        }
        catch (IOException exception)
        {
            error = exception.Message;
            return false;
        }
        catch (ArgumentException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    /// <summary>尝试读取文件长度；失败时返回 false。</summary>
    public static bool TryGetFileLength(
        string path,
        out long length,
        out string error)
    {
        length = 0;
        error = string.Empty;

        try
        {
            FileInfo info = new FileInfo(path);

            if (!info.Exists)
            {
                return false;
            }

            length = info.Length;
            return true;
        }
        catch (UnauthorizedAccessException exception)
        {
            error = exception.Message;
            return false;
        }
        catch (IOException exception)
        {
            error = exception.Message;
            return false;
        }
        catch (ArgumentException exception)
        {
            error = exception.Message;
            return false;
        }
    }

    /// <summary>安全测量目录中的文件数，不跟随重解析点。</summary>
    public static CalculationWorkflowArtifactDirectoryMeasurement
        MeasureDirectory(string directory)
    {
        CalculationWorkflowArtifactDirectoryMeasurement measurement =
            new CalculationWorkflowArtifactDirectoryMeasurement();
        MeasureDirectoryRecursive(directory, measurement);
        return measurement;
    }

    private static void MeasureDirectoryRecursive(
        string directory,
        CalculationWorkflowArtifactDirectoryMeasurement measurement)
    {
        if (!TryEnumerate(directory, out List<CalculationWorkflowArtifactPathEntry> entries, out string error))
        {
            measurement.Error = error;
            return;
        }

        for (int index = 0; index < entries.Count; index++)
        {
            CalculationWorkflowArtifactPathEntry entry = entries[index];

            if (!string.IsNullOrWhiteSpace(entry.AttributesError))
            {
                measurement.Error = entry.AttributesError;
                continue;
            }

            if (entry.IsReparsePoint)
            {
                measurement.ReparsePointPaths.Add(entry.FullPath);
                continue;
            }

            if (entry.IsDirectory)
            {
                MeasureDirectoryRecursive(entry.FullPath, measurement);
                continue;
            }

            if (TryGetFileLength(
                    entry.FullPath,
                    out long length,
                    out string fileError))
            {
                measurement.FileCount++;
                measurement.TotalBytes += length;
            }
            else if (!string.IsNullOrWhiteSpace(fileError))
            {
                measurement.Error = fileError;
            }
        }
    }

    private static bool IsWithin(
        string rootFullPath,
        string candidateFullPath)
    {
        string root = NormalizeDirectory(rootFullPath);
        string candidate = Path.GetFullPath(candidateFullPath);

        if (string.Equals(root, candidate, PathComparison))
        {
            return true;
        }

        return candidate.StartsWith(
            root + Path.DirectorySeparatorChar,
            PathComparison)
            || candidate.StartsWith(
                root + Path.AltDirectorySeparatorChar,
                PathComparison);
    }

    private static string NormalizeDirectory(string path)
    {
        return Path.GetFullPath(path).TrimEnd(
            Path.DirectorySeparatorChar,
            Path.AltDirectorySeparatorChar);
    }

    private static bool IsReparsePoint(string path)
    {
        FileAttributes attributes = File.GetAttributes(path);
        return attributes.HasFlag(FileAttributes.ReparsePoint);
    }
}
