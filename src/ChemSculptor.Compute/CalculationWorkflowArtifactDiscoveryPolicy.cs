namespace ChemSculptor.Compute;

/// <summary>产物发现的排除判定。</summary>
internal sealed class CalculationWorkflowArtifactExclusionDecision
{
    /// <summary>排除类别。</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>排除原因。</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>是否可以重新生成。</summary>
    public bool CanRegenerate { get; set; } = true;

    /// <summary>是否影响科学复现。</summary>
    public bool AffectsReproduction { get; set; }
}

/// <summary>
/// 全过程产物发现的通用策略。
/// 这里只认识临时文件、空控制台流和稳定文件角色，不包含 Gaussian 专用规则。
/// </summary>
internal static class CalculationWorkflowArtifactDiscoveryPolicy
{
    /// <summary>当前发现策略版本。</summary>
    public const string Version = "1.0";

    private static readonly HashSet<string> ExcludedDirectoryNames =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "scratch",
            "tmp",
            "temp",
            "cache",
            ".cache",
            "__pycache__",
            ".git"
        };

    private static readonly HashSet<string> TemporaryExtensions =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".tmp",
            ".temp",
            ".lock",
            ".lck",
            ".swp"
        };

    private static readonly HashSet<string> OperatingSystemFiles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".DS_Store",
            "Thumbs.db",
            "desktop.ini"
        };

    /// <summary>判断目录名称是否属于可再生成的缓存或临时目录。</summary>
    public static bool IsExcludedDirectory(string directoryName)
    {
        return !string.IsNullOrWhiteSpace(directoryName)
            && ExcludedDirectoryNames.Contains(directoryName);
    }

    /// <summary>取得目录排除规则；不是排除目录时返回 null。</summary>
    public static CalculationWorkflowArtifactExclusionDecision?
        GetDirectoryDecision(string directoryName)
    {
        if (!IsExcludedDirectory(directoryName))
        {
            return null;
        }

        return new CalculationWorkflowArtifactExclusionDecision
        {
            Category = "scratch-cache",
            Reason = "临时目录、scratch 或缓存不属于持久化科学证据。",
            CanRegenerate = true,
            AffectsReproduction = false
        };
    }

    /// <summary>判断文件是否属于明确排除的持久化噪声。</summary>
    public static CalculationWorkflowArtifactExclusionDecision?
        GetFileDecision(
            string logicalPath,
            string fileName,
            long? length,
            CalculationWorkflowArtifactRole role)
    {
        if (OperatingSystemFiles.Contains(fileName))
        {
            return new CalculationWorkflowArtifactExclusionDecision
            {
                Category = "operating-system-cache",
                Reason = "操作系统生成的目录元数据文件不属于计算证据。",
                CanRegenerate = true,
                AffectsReproduction = false
            };
        }

        string extension = Path.GetExtension(fileName);

        if (TemporaryExtensions.Contains(extension)
            || fileName.EndsWith("~", StringComparison.Ordinal))
        {
            return new CalculationWorkflowArtifactExclusionDecision
            {
                Category = "temporary-file",
                Reason = "临时交换文件不属于计算证据。",
                CanRegenerate = true,
                AffectsReproduction = false
            };
        }

        if (length == 0
            && (role == CalculationWorkflowArtifactRole.ConsoleOutput
                || IsConsoleFileName(fileName)))
        {
            return new CalculationWorkflowArtifactExclusionDecision
            {
                Category = "empty-console-stream",
                Reason = "零字节标准输出或标准错误没有诊断价值。",
                CanRegenerate = true,
                AffectsReproduction = false
            };
        }

        if (logicalPath.StartsWith(
                "run/scratch/",
                StringComparison.OrdinalIgnoreCase))
        {
            return new CalculationWorkflowArtifactExclusionDecision
            {
                Category = "scratch-cache",
                Reason = "Gaussian scratch 目录中的文件不属于持久化证据。",
                CanRegenerate = true,
                AffectsReproduction = false
            };
        }

        return null;
    }

    /// <summary>把程序适配器的通用产物用途映射到档案角色。</summary>
    public static CalculationWorkflowArtifactRole MapRole(
        CalculationArtifactKind kind,
        CalculationArtifactPatternScope scope,
        string logicalPath)
    {
        switch (kind)
        {
            case CalculationArtifactKind.Input:
                return scope == CalculationArtifactPatternScope.InputDirectory
                    ? CalculationWorkflowArtifactRole.SourceInput
                    : CalculationWorkflowArtifactRole.ProgramInput;
            case CalculationArtifactKind.PrimaryOutput:
                return CalculationWorkflowArtifactRole.PrimaryOutput;
            case CalculationArtifactKind.SupportingOutput:
                return IsConsolePath(logicalPath)
                    ? CalculationWorkflowArtifactRole.ConsoleOutput
                    : CalculationWorkflowArtifactRole.PostProcessOutput;
            case CalculationArtifactKind.RestartState:
                return CalculationWorkflowArtifactRole.RestartState;
            default:
                return InferExistingFileRole(logicalPath);
        }
    }

    /// <summary>根据逻辑路径推断未知持久化文件的稳定角色。</summary>
    public static CalculationWorkflowArtifactRole InferExistingFileRole(
        string logicalPath)
    {
        string normalized = logicalPath.Replace('\\', '/');
        string fileName = Path.GetFileName(normalized);
        string extension = Path.GetExtension(fileName);

        if (string.Equals(
                fileName,
                CalculationWorkspacePaths.JobManifestFileName,
                StringComparison.OrdinalIgnoreCase))
        {
            return CalculationWorkflowArtifactRole.JobManifest;
        }

        if (string.Equals(
                fileName,
                CalculationWorkspacePaths.JobParametersFileName,
                StringComparison.OrdinalIgnoreCase))
        {
            return CalculationWorkflowArtifactRole.ParameterAudit;
        }

        if (string.Equals(
                fileName,
                CalculationWorkspacePaths.JobResultFileName,
                StringComparison.OrdinalIgnoreCase))
        {
            return CalculationWorkflowArtifactRole.NormalizedResult;
        }

        if (string.Equals(
                fileName,
                CalculationWorkspacePaths.JobValidationFileName,
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                fileName,
                "validation-report.json",
                StringComparison.OrdinalIgnoreCase))
        {
            return CalculationWorkflowArtifactRole.ValidationEvidence;
        }

        if (fileName.EndsWith(
                "processing-plan.json",
                StringComparison.OrdinalIgnoreCase))
        {
            return CalculationWorkflowArtifactRole.ProcessingPlan;
        }

        if (string.Equals(
                fileName,
                "checksums.sha256",
                StringComparison.OrdinalIgnoreCase))
        {
            return CalculationWorkflowArtifactRole.Checksum;
        }

        if (IsConsoleFileName(fileName))
        {
            return CalculationWorkflowArtifactRole.ConsoleOutput;
        }

        if (string.Equals(extension, ".chk", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".fchk", StringComparison.OrdinalIgnoreCase)
            || string.Equals(extension, ".wfn", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationWorkflowArtifactRole.RestartState;
        }

        if (string.Equals(extension, ".xyz", StringComparison.OrdinalIgnoreCase))
        {
            return normalized.StartsWith(
                    "input/",
                    StringComparison.OrdinalIgnoreCase)
                ? CalculationWorkflowArtifactRole.GeometryInput
                : CalculationWorkflowArtifactRole.PostProcessOutput;
        }

        if (normalized.StartsWith(
                "input/",
                StringComparison.OrdinalIgnoreCase))
        {
            return CalculationWorkflowArtifactRole.SourceInput;
        }

        if (normalized.StartsWith(
                "run/",
                StringComparison.OrdinalIgnoreCase)
            && (string.Equals(extension, ".gjf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".com", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".inp", StringComparison.OrdinalIgnoreCase)))
        {
            return CalculationWorkflowArtifactRole.ProgramInput;
        }

        if (normalized.StartsWith(
                "run/",
                StringComparison.OrdinalIgnoreCase)
            && (string.Equals(extension, ".log", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".out", StringComparison.OrdinalIgnoreCase)))
        {
            return CalculationWorkflowArtifactRole.PrimaryOutput;
        }

        if (normalized.StartsWith(
                "results/postprocess/",
                StringComparison.OrdinalIgnoreCase))
        {
            return CalculationWorkflowArtifactRole.PostProcessOutput;
        }

        if (normalized.StartsWith(
                "results/scientific-data/",
                StringComparison.OrdinalIgnoreCase))
        {
            return CalculationWorkflowArtifactRole.ScientificData;
        }

        if (normalized.StartsWith(
                "results/reports/",
                StringComparison.OrdinalIgnoreCase))
        {
            return CalculationWorkflowArtifactRole.Report;
        }

        if (string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase))
        {
            return CalculationWorkflowArtifactRole.Narrative;
        }

        return CalculationWorkflowArtifactRole.Other;
    }

    private static bool IsConsolePath(string logicalPath)
    {
        return IsConsoleFileName(Path.GetFileName(logicalPath));
    }

    private static bool IsConsoleFileName(string fileName)
    {
        return string.Equals(
                fileName,
                "stdout.log",
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                fileName,
                "stderr.log",
                StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                fileName,
                "console.log",
                StringComparison.OrdinalIgnoreCase);
    }
}
