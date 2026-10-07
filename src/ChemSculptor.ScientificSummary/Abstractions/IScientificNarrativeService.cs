using ChemSculptor.ScientificSummary.Models;

namespace ChemSculptor.ScientificSummary.Abstractions;

/// <summary>读取科学数据并生成叙述包。</summary>
public interface IScientificNarrativeService
{
    /// <summary>按科学成果标识生成叙述包。</summary>
    Task<ScientificNarrativePackage?> BuildAsync(
        string resultId,
        CancellationToken cancellationToken = default);
}
