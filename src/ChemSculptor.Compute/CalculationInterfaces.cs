using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Compute;

/// <summary>
/// 量子化学程序适配器。
/// 只负责输入生成、命令构建和输出解析。
/// </summary>
public interface IQuantumProgramAdapter
{
    /// <summary>程序名称。</summary>
    string ProgramName { get; }

    /// <summary>判断该适配器是否能处理指定计算方案。</summary>
    bool CanRun(CalculationSpec spec);

    /// <summary>生成程序输入文件。</summary>
    Task WriteInputAsync(
        CalculationSpec spec,
        CanonicalGeometry geometry,
        string outputPath,
        CancellationToken cancellationToken = default);

    /// <summary>构建执行上下文。</summary>
    CalculationExecutionContext BuildExecutionContext(
        CalculationJob job,
        CalculationSpec spec);

    /// <summary>解析程序输出文件。</summary>
    Task<CalculationResult> ParseOutputAsync(
        string outputPath,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 执行后端。
/// 负责本机进程或远程集群的提交、查询、取回和取消。
/// </summary>
public interface IComputeBackend
{
    /// <summary>后端名称。</summary>
    string Name { get; }

    /// <summary>提交作业。</summary>
    Task<string> SubmitAsync(
        CalculationJob job,
        CalculationExecutionContext context,
        CancellationToken cancellationToken = default);

    /// <summary>查询作业状态。</summary>
    Task<CalculationJobState> GetStatusAsync(
        CalculationJob job,
        CancellationToken cancellationToken = default);

    /// <summary>取回作业产物。</summary>
    Task FetchArtifactsAsync(
        CalculationJob job,
        string localDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>取消作业。</summary>
    Task CancelAsync(
        CalculationJob job,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 计算队列接口。
/// 当前阶段预留，不实现实际队列。
/// </summary>
public interface ICalculationQueue
{
    /// <summary>当前队列中的作业数量。</summary>
    int Count { get; }

    /// <summary>把作业加入队列。</summary>
    Task EnqueueAsync(
        CalculationJob job,
        CancellationToken cancellationToken = default);

    /// <summary>从队列取出一个作业。</summary>
    Task<CalculationJob?> DequeueAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// 计算调度器。
/// 负责并发控制和作业调度。
/// </summary>
public interface ICalculationScheduler
{
    /// <summary>最大并发数。</summary>
    int MaxConcurrency { get; }

    /// <summary>调度一个计算作业。</summary>
    Task ScheduleAsync(
        CalculationJob job,
        CancellationToken cancellationToken = default);

    /// <summary>查询作业状态。</summary>
    Task<CalculationJobState> GetStatusAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>取消作业。</summary>
    Task CancelAsync(
        string jobId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 计算工作区管理接口。
/// </summary>
public interface ICalculationWorkspace
{
    /// <summary>确保几何资产目录存在。</summary>
    Task EnsureGeometryWorkspaceAsync(
        string geometryId,
        CancellationToken cancellationToken = default);

    /// <summary>确保计算作业目录存在。</summary>
    Task EnsureJobWorkspaceAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>获取几何资产目录。</summary>
    string GetGeometryDirectory(string geometryId);

    /// <summary>获取作业目录。</summary>
    string GetJobDirectory(string jobId);

    /// <summary>获取输入目录。</summary>
    string GetInputDirectory(string jobId);

    /// <summary>获取运行目录。</summary>
    string GetRunDirectory(string jobId);

    /// <summary>获取结果目录。</summary>
    string GetResultDirectory(string jobId);

    /// <summary>获取程序输出文件路径。</summary>
    string GetJobOutputPath(string jobId);

    /// <summary>获取作业清单文件路径。</summary>
    string GetJobManifestPath(string jobId);

    /// <summary>获取规范化结果文件路径。</summary>
    string GetJobResultPath(string jobId);
}

/// <summary>
/// 计算作业与结果仓储接口。
/// </summary>
public interface ICalculationRepository
{
    /// <summary>保存作业。</summary>
    Task SaveJobAsync(
        CalculationJob job,
        CancellationToken cancellationToken = default);

    /// <summary>读取作业。</summary>
    Task<CalculationJob?> GetJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>保存结果。</summary>
    Task SaveResultAsync(
        CalculationResult result,
        CancellationToken cancellationToken = default);

    /// <summary>读取结果。</summary>
    Task<CalculationResult?> GetResultAsync(
        string jobId,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 计算参数校验器。
/// </summary>
public interface ICalculationParameterValidator
{
    /// <summary>校验计算方案。</summary>
    Task<CalculationValidationReport> ValidateAsync(
        CalculationSpec spec,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 科学风险评估器。
/// </summary>
public interface IScientificRiskEvaluator
{
    /// <summary>评估计算方案的科学风险。</summary>
    Task<RiskAssessment> EvaluateAsync(
        CalculationSpec spec,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 人工审批服务。
/// 预留参数确认、风险确认和恢复审批。
/// </summary>
public interface IApprovalService
{
    /// <summary>创建审批问题。</summary>
    Task<CalculationQuestion> RequestApprovalAsync(
        CalculationJob job,
        RiskAssessment riskAssessment,
        CancellationToken cancellationToken = default);

    /// <summary>提交审批决定。</summary>
    Task SubmitDecisionAsync(
        ApprovalDecision decision,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 计算计划器。
/// 负责把用户请求转换为计算方案和待确认问题。
/// </summary>
public interface ICalculationPlanner
{
    /// <summary>生成计算计划。</summary>
    Task<CalculationPlan> PlanAsync(
        CalculationRequest request,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// 单点计算服务。
/// 负责串联计算作业的创建、执行、解析和结果查询。
/// </summary>
public interface ISinglePointCalculationService
{
    /// <summary>提交单点计算。</summary>
    Task<CalculationJob> SubmitAsync(
        CalculationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>查询计算作业。</summary>
    Task<CalculationJob?> GetJobAsync(
        string jobId,
        CancellationToken cancellationToken = default);

    /// <summary>查询计算结果。</summary>
    Task<CalculationResult?> GetResultAsync(
        string jobId,
        CancellationToken cancellationToken = default);
}
