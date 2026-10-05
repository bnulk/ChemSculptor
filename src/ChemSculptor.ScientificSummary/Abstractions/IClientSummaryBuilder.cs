using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificSummary.Models;

namespace ChemSculptor.ScientificSummary.Abstractions;

/// <summary>从科学成果生成客户端摘要。</summary>
public interface IClientSummaryBuilder
{
    /// <summary>生成客户端摘要。</summary>
    Task<ClientScientificSummary> BuildAsync(
        ScientificResult result,
        CancellationToken cancellationToken = default);
}
