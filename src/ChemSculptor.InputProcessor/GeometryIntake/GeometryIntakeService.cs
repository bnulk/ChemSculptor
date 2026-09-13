namespace ChemSculptor.InputProcessor.GeometryIntake;

/// <summary>
/// 几何接收流水线的最终结果。
/// </summary>
public sealed class GeometryIntakeResult
{
    /// <summary>保存后的几何资产。</summary>
    public GeometryAsset? Asset { get; set; }

    /// <summary>解析结果。</summary>
    public GeometryParseResult? ParseResult { get; set; }

    /// <summary>验证报告；未解析出几何时为空。</summary>
    public GeometryValidationReport? ValidationReport { get; set; }
}

/// <summary>
/// 几何接收服务。
/// 负责串联“原始提交 → 选择解析器 → 解析 → 验证 → 保存资产”。
/// 它不实现任何具体解析规则。
/// </summary>
public sealed class GeometryIntakeService
{
    private readonly GeometryParserRegistry _parserRegistry;
    private readonly IGeometryValidator _validator;
    private readonly IGeometryAssetRepository _repository;

    /// <summary>创建几何接收服务。</summary>
    public GeometryIntakeService(
        GeometryParserRegistry parserRegistry,
        IGeometryValidator validator,
        IGeometryAssetRepository repository)
    {
        _parserRegistry = parserRegistry;
        _validator = validator;
        _repository = repository;
    }

    /// <summary>
    /// 处理一次几何提交。
    /// 当前解析器和验证器均为骨架，因此不会产生真实几何结果。
    /// </summary>
    public async Task<GeometryIntakeResult> SubmitAsync(
        RawGeometrySubmission submission,
        CancellationToken cancellationToken = default)
    {
        GeometryParseResult parseResult =
            await _parserRegistry.ParseAsync(submission, cancellationToken);

        GeometryValidationReport? validationReport = null;

        if (parseResult.Geometry != null)
        {
            validationReport = await _validator.ValidateAsync(parseResult.Geometry, cancellationToken);
        }

        GeometryAsset asset = new GeometryAsset();
        asset.Id = submission.Id;
        asset.Submission = submission;
        asset.ParseResult = parseResult;
        asset.ValidationReport = validationReport;
        asset.CreatedAt = DateTimeOffset.UtcNow;

        await _repository.SaveAsync(asset, cancellationToken);

        GeometryIntakeResult intakeResult = new GeometryIntakeResult();
        intakeResult.Asset = asset;
        intakeResult.ParseResult = parseResult;
        intakeResult.ValidationReport = validationReport;

        return intakeResult;
    }
}
