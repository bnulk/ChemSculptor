namespace ChemSculptor.ScientificData.Extraction.ArtifactResolution.Models;

/// <summary>已通过路径和校验验证的可读取科学点文件。</summary>
public sealed class ScientificArtifactContent
{
    /// <summary>科学成果标识。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>科学点标识。</summary>
    public string PointId { get; set; } = string.Empty;

    /// <summary>文件引用标识。</summary>
    public string ArtifactId { get; set; } = string.Empty;

    /// <summary>产生该文件的计算作业标识。</summary>
    public string CalculationJobId { get; set; } = string.Empty;

    /// <summary>下载时使用的文件名。</summary>
    public string DownloadFileName { get; set; } = string.Empty;

    /// <summary>文件媒体类型。</summary>
    public string MediaType { get; set; } =
        "application/octet-stream";

    /// <summary>文件字节数。</summary>
    public long Length { get; set; }

    /// <summary>已验证的 SHA-256 摘要。</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>从文件起点开始的可读流。</summary>
    public Stream Content { get; set; } = Stream.Null;
}
