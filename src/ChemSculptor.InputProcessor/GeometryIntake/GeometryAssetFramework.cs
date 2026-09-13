using System.Collections.Concurrent;

namespace ChemSculptor.InputProcessor.GeometryIntake;

/// <summary>
/// 几何资产。
/// 保存原始提交、解析结果和验证报告，供后续计算引用与溯源。
/// </summary>
public sealed class GeometryAsset
{
    /// <summary>几何资产标识。</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>原始提交内容。</summary>
    public RawGeometrySubmission? Submission { get; set; }

    /// <summary>解析结果。</summary>
    public GeometryParseResult? ParseResult { get; set; }

    /// <summary>验证报告；尚未验证时为空。</summary>
    public GeometryValidationReport? ValidationReport { get; set; }

    /// <summary>资产创建时间。</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 几何资产仓储契约。
/// </summary>
public interface IGeometryAssetRepository
{
    /// <summary>保存几何资产。</summary>
    Task SaveAsync(GeometryAsset asset, CancellationToken cancellationToken = default);

    /// <summary>按标识读取几何资产。</summary>
    Task<GeometryAsset?> GetAsync(string assetId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 内存版几何资产仓储。
/// </summary>
public sealed class InMemoryGeometryAssetRepository : IGeometryAssetRepository
{
    private readonly ConcurrentDictionary<string, GeometryAsset> _assets =
        new ConcurrentDictionary<string, GeometryAsset>(StringComparer.OrdinalIgnoreCase);

    /// <summary>保存几何资产。</summary>
    public Task SaveAsync(GeometryAsset asset, CancellationToken cancellationToken = default)
    {
        _assets[asset.Id] = asset;
        return Task.CompletedTask;
    }

    /// <summary>按标识读取几何资产。</summary>
    public Task<GeometryAsset?> GetAsync(string assetId, CancellationToken cancellationToken = default)
    {
        GeometryAsset? asset;
        if (_assets.TryGetValue(assetId, out asset))
        {
            return Task.FromResult<GeometryAsset?>(asset);
        }

        return Task.FromResult<GeometryAsset?>(null);
    }
}
