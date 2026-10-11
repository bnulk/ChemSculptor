namespace ChemSculptor.Compute;

/// <summary>输入文件生成技能请求。</summary>
public class CalculationInputGenerationRequest
{
    /// <summary>计算作业。</summary>
    public CalculationJob Job { get; set; } = new CalculationJob();

    /// <summary>计算方案。</summary>
    public CalculationSpec Spec { get; set; } = new CalculationSpec();

    /// <summary>客户端提供的坐标文本。</summary>
    public string CoordinateText { get; set; } = string.Empty;

    /// <summary>原始输入文件路径。</summary>
    public string InputFilePath { get; set; } = string.Empty;

    /// <summary>运行目录中的输入文件路径。</summary>
    public string RunInputFilePath { get; set; } = string.Empty;

    /// <summary>程序输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;
}

/// <summary>输入文件生成技能结果。</summary>
public class CalculationInputGenerationResult
{
    /// <summary>是否成功。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>更新后的计算作业。</summary>
    public CalculationJob Job { get; set; } = new CalculationJob();

    /// <summary>程序执行上下文。</summary>
    public CalculationExecutionContext ExecutionContext { get; set; } =
        new CalculationExecutionContext();

    /// <summary>诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();
}

/// <summary>计算结果提取技能请求。</summary>
public class CalculationResultExtractionRequest
{
    /// <summary>作业标识。</summary>
    public string JobId { get; set; } = string.Empty;

    /// <summary>计算程序名称。</summary>
    public string Program { get; set; } = string.Empty;

    /// <summary>输出文件路径。</summary>
    public string OutputFilePath { get; set; } = string.Empty;

    /// <summary>原始计算方案。</summary>
    public CalculationSpec Spec { get; set; } = new CalculationSpec();
}

/// <summary>计算结果提取技能结果。</summary>
public class CalculationResultExtractionResult
{
    /// <summary>是否成功。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>通用计算结果。</summary>
    public CalculationResult Result { get; set; } = new CalculationResult();

    /// <summary>诊断信息。</summary>
    public List<string> Diagnostics { get; set; } = new List<string>();
}

/// <summary>计算结果验证技能请求。</summary>
public class CalculationResultValidationRequest
{
    /// <summary>计算作业。</summary>
    public CalculationJob Job { get; set; } = new CalculationJob();

    /// <summary>通用计算结果。</summary>
    public CalculationResult Result { get; set; } = new CalculationResult();
}

/// <summary>计算结果验证技能结果。</summary>
public class CalculationResultValidationResult
{
    /// <summary>是否通过验证。</summary>
    public bool Passed { get; set; }

    /// <summary>验证报告。</summary>
    public CalculationValidationReport Report { get; set; } =
        new CalculationValidationReport();
}

/// <summary>计算提交技能结果。</summary>
public class CalculationSubmissionSkillResult
{
    /// <summary>是否成功。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>已提交的作业。</summary>
    public CalculationJob Job { get; set; } = new CalculationJob();

    /// <summary>执行上下文。</summary>
    public CalculationExecutionContext ExecutionContext { get; set; } =
        new CalculationExecutionContext();
}

/// <summary>计算等待技能结果。</summary>
public class CalculationWaitSkillResult
{
    /// <summary>最终作业状态。</summary>
    public CalculationJobState State { get; set; } =
        CalculationJobState.Running;

    /// <summary>更新后的作业。</summary>
    public CalculationJob Job { get; set; } = new CalculationJob();
}

/// <summary>工作流结果验证技能结果。</summary>
public class CalculationWorkflowValidationSkillResult
{
    /// <summary>是否通过。</summary>
    public bool Passed { get; set; }

    /// <summary>更新后的作业。</summary>
    public CalculationJob Job { get; set; } = new CalculationJob();

    /// <summary>验证报告。</summary>
    public CalculationValidationReport Report { get; set; } =
        new CalculationValidationReport();
}

/// <summary>单点计算 Skill 的步骤结果。</summary>
public class SinglePointCalculationStepResult
{
    /// <summary>步骤名称。</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>调用的原子 Skill 标识。</summary>
    public string SkillId { get; set; } = string.Empty;

    /// <summary>步骤是否执行成功。</summary>
    public bool Succeeded { get; set; }

    /// <summary>步骤失败诊断。</summary>
    public string Diagnostics { get; set; } = string.Empty;

    /// <summary>步骤开始时间。</summary>
    public DateTimeOffset StartedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>步骤结束时间。</summary>
    public DateTimeOffset CompletedAt { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>
/// 一次稳定单点计算的结果契约。
/// 同时兼容工作流验证和结果提取所需的上游字段。
/// </summary>
public class SinglePointCalculationSkillResult
{
    /// <summary>计算和结果提取是否成功。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>必要验证是否通过。</summary>
    public bool Passed { get; set; }

    /// <summary>计算作业。</summary>
    public CalculationJob Job { get; set; } = new CalculationJob();

    /// <summary>通用计算结果。</summary>
    public CalculationResult Result { get; set; } = new CalculationResult();

    /// <summary>结果验证报告。</summary>
    public CalculationValidationReport Report { get; set; } =
        new CalculationValidationReport();

    /// <summary>等待结果。</summary>
    public CalculationWaitSkillResult Wait { get; set; } =
        new CalculationWaitSkillResult();

    /// <summary>原子步骤执行轨迹。</summary>
    public List<SinglePointCalculationStepResult> Steps { get; set; } =
        new List<SinglePointCalculationStepResult>();
}

/// <summary>工作流处理方案技能结果。</summary>
public class CalculationWorkflowProcessingPlanSkillResult
{
    /// <summary>通用处理方案。</summary>
    public CalculationProcessingPlan Plan { get; set; } =
        new CalculationProcessingPlan();
}

/// <summary>程序处理方案翻译技能请求。</summary>
public class ProgramProcessingPlanTranslationRequest
{
    /// <summary>计算作业。</summary>
    public CalculationJob Job { get; set; } = new CalculationJob();

    /// <summary>通用处理方案。</summary>
    public CalculationProcessingPlan ProcessingPlan { get; set; } =
        new CalculationProcessingPlan();
}

/// <summary>程序处理方案翻译技能结果。</summary>
public class ProgramProcessingPlanTranslationResult
{
    /// <summary>是否成功。</summary>
    public bool Succeeded { get; set; }

    /// <summary>失败说明。</summary>
    public string Error { get; set; } = string.Empty;

    /// <summary>程序专用处理方案。</summary>
    public ProgramProcessingPlan ProgramPlan { get; set; } =
        new ProgramProcessingPlan();
}
