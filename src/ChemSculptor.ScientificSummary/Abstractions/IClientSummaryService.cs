using ChemSculptor.ScientificSummary.Models;

namespace ChemSculptor.ScientificSummary.Abstractions;

/// <summary>从科学数据仓储生成并发送客户端摘要。</summary>
public interface IClientSummaryService
{
    /// <summary>生成并发送一项科学成果的客户端摘要。</summary>
    Task<ClientScientificSummary?> BuildAndSendAsync(
        string resultId,
        string clientId,
        CancellationToken cancellationToken = default);
}
