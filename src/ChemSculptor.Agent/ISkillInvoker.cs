namespace ChemSculptor.Agent;

/// <summary>
/// 通用技能调用器。
/// Agent 只通过技能标识和通用请求模型调用技能。
/// </summary>
public interface ISkillInvoker
{
    /// <summary>调用一个 JSON 请求/结果技能。</summary>
    Task<TResult> InvokeAsync<TRequest, TResult>(
        string skillId,
        TRequest request,
        CancellationToken cancellationToken = default)
        where TRequest : class
        where TResult : class;
}
