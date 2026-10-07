using ChemSculptor.ScientificData.Models;

namespace ChemSculptor.ScientificData.Extraction.ArtifactResolution.Models;

/// <summary>单个科学点文件引用的解析结果。</summary>
public sealed class ScientificArtifactFileManifest
{
    /// <summary>文件引用标识。</summary>
    public string ArtifactId { get; set; } = string.Empty;

    /// <summary>科学点标识。</summary>
    public string PointId { get; set; } = string.Empty;

    /// <summary>产生该文件的计算作业标识。</summary>
    public string CalculationJobId { get; set; } = string.Empty;

    /// <summary>文件通用类别。</summary>
    public ScientificArtifactKind Kind { get; set; } =
        ScientificArtifactKind.Other;

    /// <summary>相对于计算运行目录的路径。</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>下载时使用的文件名。</summary>
    public string DownloadFileName { get; set; } = string.Empty;

    /// <summary>文件媒体类型。</summary>
    public string MediaType { get; set; } =
        "application/octet-stream";

    /// <summary>文件字节数。</summary>
    public long Length { get; set; }

    /// <summary>文件引用中记录的 SHA-256 摘要。</summary>
    public string ExpectedSha256 { get; set; } = string.Empty;

    /// <summary>实际计算得到的 SHA-256 摘要。</summary>
    public string ActualSha256 { get; set; } = string.Empty;

    /// <summary>SHA-256 摘要是否一致。</summary>
    public bool IsSha256Valid { get; set; }

    /// <summary>是否允许下载该文件。</summary>
    public bool CanDownload { get; set; }

    /// <summary>是否可以用于恢复或继续计算。</summary>
    public bool CanUseForRestart { get; set; }

    /// <summary>文件是否已解析、存在且通过 SHA-256 校验。</summary>
    public bool IsAvailable { get; set; }

    /// <summary>文件级错误；没有错误时为空字符串。</summary>
    public string Error { get; set; } = string.Empty;
}
