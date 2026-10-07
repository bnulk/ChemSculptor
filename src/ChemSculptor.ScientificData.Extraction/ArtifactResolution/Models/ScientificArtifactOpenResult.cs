namespace ChemSculptor.ScientificData.Extraction.ArtifactResolution.Models;

/// <summary>打开科学点文件的结果。</summary>
public sealed class ScientificArtifactOpenResult
{
    /// <summary>是否成功打开文件。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明；成功时为空字符串。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>成功打开的文件内容。</summary>
    public ScientificArtifactContent? Artifact { get; set; }
}
