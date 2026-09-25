namespace ChemSculptor.Agent;

/// <summary>
/// 智能体编排服务。
/// 负责把会话意图转换为具体计算执行。
/// </summary>
public interface IAgentService
{
    /// <summary>处理客户端原始消息。</summary>
    Task<AgentResult> HandleMessageAsync(
        AgentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>直接执行单点计算。</summary>
    Task<AgentResult> ExecuteSinglePointAsync(
        AgentSinglePointRequest request,
        CancellationToken cancellationToken = default);
}
