using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificSummary.Models;

namespace ChemSculptor.ScientificSummary.Abstractions;

/// <summary>从科学数据生成叙述性文本。</summary>
public interface IScientificNarrativeBuilder
{
    /// <summary>生成科学成果叙述包。</summary>
    Task<ScientificNarrativePackage> BuildAsync(
        ScientificResult result,
        CancellationToken cancellationToken = default);
}
