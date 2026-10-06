namespace ChemSculptor.ScientificData.Models;

/// <summary>科学点对应的一项原始文件引用。</summary>
public sealed class PointArtifactReference
{
    /// <summary>文件引用标识。</summary>
    public string ArtifactId { get; set; } = string.Empty;

    /// <summary>产生该文件的计算作业标识。</summary>
    public string CalculationJobId { get; set; } = string.Empty;

    /// <summary>文件通用类别。</summary>
    public ScientificArtifactKind Kind { get; set; } =
        ScientificArtifactKind.Other;

    /// <summary>相对于计算运行目录的路径。</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>下载时使用的文件名。</summary>
    public string DownloadFileName { get; set; } = string.Empty;

    /// <summary>不包含扩展名的规范文件名主体。</summary>
    public string CanonicalStem { get; set; } = string.Empty;

    /// <summary>规范文件扩展名，包含前导点。</summary>
    public string CanonicalExtension { get; set; } = string.Empty;

    /// <summary>文件媒体类型。</summary>
    public string MediaType { get; set; } =
        "application/octet-stream";

    /// <summary>文件字节数。</summary>
    public long Length { get; set; }

    /// <summary>文件的 SHA-256 摘要。</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>是否可以下载。</summary>
    public bool CanDownload { get; set; } = true;

    /// <summary>是否可以用于恢复或继续计算。</summary>
    public bool CanUseForRestart { get; set; }

    /// <summary>附加信息。</summary>
    public Dictionary<string, string> Metadata { get; set; } =
        new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);
}
