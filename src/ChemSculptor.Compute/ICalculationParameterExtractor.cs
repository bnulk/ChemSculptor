namespace ChemSculptor.Compute;

/// <summary>对话计算参数提取器。</summary>
public interface ICalculationParameterExtractor
{
    /// <summary>从用户文本提取参数意图。</summary>
    Task<CalculationParameterExtractionResult> ExtractAsync(
        string text,
        CalculationParameterExtractionContext context,
        CancellationToken cancellationToken = default);
}
