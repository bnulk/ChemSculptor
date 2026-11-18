namespace ChemSculptor.Agent;

/// <summary>
/// 科学成果、科学点与根工作流作业的关联审计服务。
/// 只读取已有的科学数据和作业清单，不复制文件、不执行计算。
/// </summary>
public interface IScientificWorkflowInventoryService
{
    /// <summary>构建当前根工作流的科学成果关联清单。</summary>
    Task<ScientificWorkflowInventory> QueryAsync(
        ScientificWorkflowInventoryQuery query,
        CancellationToken cancellationToken = default);
}
