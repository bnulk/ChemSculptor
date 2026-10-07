namespace ChemSculptor.ScientificData.Extraction.ArtifactResolution.Models;

/// <summary>科学成果中全部科学点文件引用的解析清单。</summary>
public sealed class ScientificArtifactManifest
{
    /// <summary>科学成果标识。</summary>
    public string ResultId { get; set; } = string.Empty;

    /// <summary>根工作流标识。</summary>
    public string RootWorkflowId { get; set; } = string.Empty;

    /// <summary>是否成功读取并解析科学成果。</summary>
    public bool Succeeded { get; set; }

    /// <summary>清单级错误；没有错误时为空字符串。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>按科学成果中的顺序排列的科学点清单。</summary>
    public List<ScientificArtifactPointManifest> Points { get; set; } =
        new List<ScientificArtifactPointManifest>();
}
