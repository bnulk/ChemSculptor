namespace ChemSculptor.Domain;

/// <summary>
/// 规则引擎。
/// 负责在工作流提交前进行校验，返回违规原因列表；空列表表示通过。
/// </summary>
public interface IRuleEngine
{
    /// <summary>校验工作流定义。</summary>
    Task<IReadOnlyList<string>> ValidateWorkflowAsync(
        WorkflowDefinition definition,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 验证门。
/// 负责在节点执行后检查结果是否可信，通过后才允许下游节点继续。
/// </summary>
public interface IValidationGate
{
    /// <summary>验证任务结果。</summary>
    Task<ValidationReport> ValidateAsync(TaskResult result, CancellationToken cancellationToken = default);
}

/// <summary>
/// 验证门返回的质检报告。
/// </summary>
public sealed class ValidationReport
{
    /// <summary>验证结论，例如 Passed、PassedWithWarnings 或 Failed。</summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>验证置信度，取值范围 0 到 1。</summary>
    public double Confidence { get; set; }

    /// <summary>各检查项的说明。</summary>
    public List<string> Checks { get; set; } = new List<string>();
}

/// <summary>
/// 案例记忆。
/// 记录运行结果，并在以后提供相似案例检索。
/// </summary>
public interface ICaseMemory
{
    /// <summary>记录一次运行。</summary>
    Task RecordAsync(WorkflowRun run, CancellationToken cancellationToken = default);

    /// <summary>按查询条件检索相似案例。</summary>
    Task<IReadOnlyList<string>> SearchAsync(string query, CancellationToken cancellationToken = default);
}

/// <summary>
/// 大语言模型网关。
/// 模型只提供建议，不直接执行任务。
/// </summary>
public interface ILlmGateway
{
    /// <summary>根据目标生成工作流草案。</summary>
    Task<string> SuggestWorkflowAsync(string goal, CancellationToken cancellationToken = default);
}
