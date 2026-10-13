namespace ChemSculptor.Compute;

/// <summary>
/// 程序适配器可选的优化输出解析能力。
/// 通用提取节点通过该接口获取 OptimizationResult。
/// </summary>
public interface IOptimizationOutputParser
{
    /// <summary>解析几何优化输出。</summary>
    Task<OptimizationResult> ParseOptimizationOutputAsync(
        string outputPath,
        CancellationToken cancellationToken = default);
}
