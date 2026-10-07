using ChemSculptor.ScientificData.Extraction.ArtifactResolution.Models;

namespace ChemSculptor.ScientificData.Extraction.ArtifactResolution;

/// <summary>
/// 科学点文件解析服务。
/// 负责把科学成果中的相对文件引用解析为服务器真实文件。
/// </summary>
public interface IScientificArtifactResolver
{
    /// <summary>解析一项科学成果中全部科学点的文件引用。</summary>
    Task<ScientificArtifactManifest> ResolveManifestAsync(
        string resultId,
        CancellationToken cancellationToken = default);

    /// <summary>打开一个已经通过路径和 SHA-256 验证的科学点文件。</summary>
    Task<ScientificArtifactOpenResult> OpenArtifactAsync(
        string resultId,
        string pointId,
        string artifactId,
        CancellationToken cancellationToken = default);
}
