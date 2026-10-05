using ChemSculptor.ScientificSummary.Models;

namespace ChemSculptor.ScientificSummary.Abstractions;

/// <summary>把客户端摘要发送到指定客户端。</summary>
public interface IClientSummarySender
{
    /// <summary>发送客户端摘要。</summary>
    Task SendAsync(
        string clientId,
        ClientScientificSummary summary,
        CancellationToken cancellationToken = default);
}
