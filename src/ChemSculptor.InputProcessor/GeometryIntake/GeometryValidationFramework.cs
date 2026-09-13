namespace ChemSculptor.InputProcessor.GeometryIntake;

/// <summary>
/// 一条几何验证问题。
/// </summary>
public sealed class GeometryValidationIssue
{
    /// <summary>严重程度。</summary>
    public GeometryDiagnosticSeverity Severity { get; set; } = GeometryDiagnosticSeverity.Info;

    /// <summary>问题代码。</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>面向人的说明。</summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// 几何验证报告。
/// </summary>
public sealed class GeometryValidationReport
{
    /// <summary>验证状态。</summary>
    public GeometryValidationStatus Status { get; set; } = GeometryValidationStatus.NotImplemented;

    /// <summary>验证问题列表。</summary>
    public List<GeometryValidationIssue> Issues { get; set; } = new List<GeometryValidationIssue>();
}

/// <summary>
/// 几何验证器契约。
/// </summary>
public interface IGeometryValidator
{
    /// <summary>验证规范几何模型。</summary>
    Task<GeometryValidationReport> ValidateAsync(
        CanonicalGeometry geometry,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 验证器骨架。
/// 当前只建立框架，不实现任何具体验证规则。
/// </summary>
public sealed class SkeletonGeometryValidator : IGeometryValidator
{
    /// <summary>返回“尚未实现”的验证报告。</summary>
    public Task<GeometryValidationReport> ValidateAsync(
        CanonicalGeometry geometry,
        CancellationToken cancellationToken = default)
    {
        GeometryValidationReport report = new GeometryValidationReport();
        report.Status = GeometryValidationStatus.NotImplemented;

        GeometryValidationIssue issue = new GeometryValidationIssue();
        issue.Severity = GeometryDiagnosticSeverity.Warning;
        issue.Code = "VALIDATOR_NOT_IMPLEMENTED";
        issue.Message = "验证器框架已就位，但尚未实现具体验证规则。";
        report.Issues.Add(issue);

        return Task.FromResult(report);
    }
}
