namespace ChemSculptor.InputProcessor.GeometryIntake;

/// <summary>
/// 几何解析结果。
/// </summary>
public sealed class GeometryParseResult
{
    /// <summary>解析状态。</summary>
    public GeometryParseStatus Status { get; set; } = GeometryParseStatus.NotImplemented;

    /// <summary>实际选择或执行的解析器名称。</summary>
    public string ParserName { get; set; } = string.Empty;

    /// <summary>解析得到的规范几何；未解析成功时为 null。</summary>
    public CanonicalGeometry? Geometry { get; set; }

    /// <summary>解析诊断信息。</summary>
    public List<GeometryDiagnostic> Diagnostics { get; set; } = new List<GeometryDiagnostic>();
}

/// <summary>
/// 几何解析器契约。
/// 每种格式实现一个解析器，解析器只负责把原始内容转换为规范几何。
/// </summary>
public interface IGeometryParser
{
    /// <summary>解析器格式名称。</summary>
    string FormatName { get; }

    /// <summary>判断该解析器是否能处理指定提交。</summary>
    bool CanParse(RawGeometrySubmission submission);

    /// <summary>解析原始提交。</summary>
    Task<GeometryParseResult> ParseAsync(
        RawGeometrySubmission submission,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 解析器骨架。
/// 当前只建立框架，不实现任何具体格式解析。
/// </summary>
public abstract class GeometryParserSkeleton : IGeometryParser
{
    /// <summary>解析器格式名称。</summary>
    public abstract string FormatName { get; }

    /// <summary>默认只根据格式提示判断是否匹配。</summary>
    public abstract bool CanParse(RawGeometrySubmission submission);

    /// <summary>
    /// 返回“尚未实现”的解析结果。
    /// 后续真实解析器应重写该方法。
    /// </summary>
    public virtual Task<GeometryParseResult> ParseAsync(
        RawGeometrySubmission submission,
        CancellationToken cancellationToken = default)
    {
        GeometryParseResult result = new GeometryParseResult();
        result.Status = GeometryParseStatus.NotImplemented;
        result.ParserName = FormatName;

        GeometryDiagnostic diagnostic = new GeometryDiagnostic();
        diagnostic.Severity = GeometryDiagnosticSeverity.Warning;
        diagnostic.Code = "PARSER_NOT_IMPLEMENTED";
        diagnostic.Message = "解析器框架已就位，但尚未实现具体解析逻辑。";
        result.Diagnostics.Add(diagnostic);

        return Task.FromResult(result);
    }
}

/// <summary>XYZ 格式解析器骨架。</summary>
public sealed class XyzGeometryParser : GeometryParserSkeleton
{
    /// <summary>格式名称。</summary>
    public override string FormatName
    {
        get { return "xyz"; }
    }

    /// <summary>仅当提示为 XYZ 时匹配。</summary>
    public override bool CanParse(RawGeometrySubmission submission)
    {
        return submission.FormatHint == GeometryFormatHint.Xyz;
    }
}

/// <summary>Gaussian 格式解析器骨架。</summary>
public sealed class GaussianGeometryParser : GeometryParserSkeleton
{
    /// <summary>格式名称。</summary>
    public override string FormatName
    {
        get { return "gaussian"; }
    }

    /// <summary>仅当提示为 Gaussian 时匹配。</summary>
    public override bool CanParse(RawGeometrySubmission submission)
    {
        return submission.FormatHint == GeometryFormatHint.Gaussian;
    }
}

/// <summary>ORCA 格式解析器骨架。</summary>
public sealed class OrcaGeometryParser : GeometryParserSkeleton
{
    /// <summary>格式名称。</summary>
    public override string FormatName
    {
        get { return "orca"; }
    }

    /// <summary>仅当提示为 ORCA 时匹配。</summary>
    public override bool CanParse(RawGeometrySubmission submission)
    {
        return submission.FormatHint == GeometryFormatHint.Orca;
    }
}

/// <summary>ONIOM 格式解析器骨架。</summary>
public sealed class OniomGeometryParser : GeometryParserSkeleton
{
    /// <summary>格式名称。</summary>
    public override string FormatName
    {
        get { return "oniom"; }
    }

    /// <summary>仅当提示为 ONIOM 时匹配。</summary>
    public override bool CanParse(RawGeometrySubmission submission)
    {
        return submission.FormatHint == GeometryFormatHint.Oniom;
    }
}

/// <summary>
/// 解析器注册表。
/// 负责按顺序选择能够处理提交的解析器。
/// </summary>
public sealed class GeometryParserRegistry
{
    private readonly List<IGeometryParser> _parsers;

    /// <summary>使用解析器集合创建注册表。</summary>
    public GeometryParserRegistry(IEnumerable<IGeometryParser> parsers)
    {
        _parsers = new List<IGeometryParser>();

        foreach (IGeometryParser parser in parsers)
        {
            _parsers.Add(parser);
        }
    }

    /// <summary>注册一个解析器。</summary>
    public void Register(IGeometryParser parser)
    {
        _parsers.Add(parser);
    }

    /// <summary>查找第一个可以处理该提交的解析器。</summary>
    public IGeometryParser? FindParser(RawGeometrySubmission submission)
    {
        for (int index = 0; index < _parsers.Count; index++)
        {
            if (_parsers[index].CanParse(submission))
            {
                return _parsers[index];
            }
        }

        return null;
    }

    /// <summary>选择解析器并执行解析。</summary>
    public async Task<GeometryParseResult> ParseAsync(
        RawGeometrySubmission submission,
        CancellationToken cancellationToken = default)
    {
        IGeometryParser? parser = FindParser(submission);

        if (parser == null)
        {
            GeometryParseResult missingResult = new GeometryParseResult();
            missingResult.Status = GeometryParseStatus.Failed;
            missingResult.ParserName = string.Empty;

            GeometryDiagnostic diagnostic = new GeometryDiagnostic();
            diagnostic.Severity = GeometryDiagnosticSeverity.Error;
            diagnostic.Code = "PARSER_NOT_FOUND";
            diagnostic.Message = "没有找到能够处理该几何格式的解析器。";
            missingResult.Diagnostics.Add(diagnostic);

            return missingResult;
        }

        return await parser.ParseAsync(submission, cancellationToken);
    }
}
