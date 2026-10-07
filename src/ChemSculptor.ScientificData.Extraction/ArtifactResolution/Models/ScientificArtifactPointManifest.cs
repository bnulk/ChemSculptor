namespace ChemSculptor.ScientificData.Extraction.ArtifactResolution.Models;

/// <summary>单个科学点的文件引用解析清单。</summary>
public sealed class ScientificArtifactPointManifest
{
    /// <summary>科学点在成果中的顺序，从 1 开始。</summary>
    public int Sequence { get; set; }

    /// <summary>科学点标识。</summary>
    public string PointId { get; set; } = string.Empty;

    /// <summary>产生该科学点的主要计算作业标识。</summary>
    public string CalculationJobId { get; set; } = string.Empty;

    /// <summary>该科学点的运行目录是否可解析并存在。</summary>
    public bool IsAvailable { get; set; }

    /// <summary>科学点级错误；没有错误时为空字符串。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>该科学点的文件引用。</summary>
    public List<ScientificArtifactFileManifest> Artifacts { get; set; } =
        new List<ScientificArtifactFileManifest>();
}
