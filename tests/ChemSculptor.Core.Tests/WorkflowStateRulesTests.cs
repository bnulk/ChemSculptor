using ChemSculptor.Core;
using ChemSculptor.Domain;

namespace ChemSculptor.Core.Tests;

/// <summary>
/// 工作流状态转换规则测试。
/// </summary>
public class WorkflowStateRulesTests
{
    /// <summary>验证 Running 可以转换到 Passed。</summary>
    [Fact]
    public void RunningCanMoveToPassed()
    {
        Assert.True(WorkflowStateRules.CanTransition(WorkflowState.Running, WorkflowState.Passed));
    }

    /// <summary>验证 Draft 不能直接跳到 Passed。</summary>
    [Fact]
    public void DraftCannotSkipToPassed()
    {
        Assert.False(WorkflowStateRules.CanTransition(WorkflowState.Draft, WorkflowState.Passed));
    }

    /// <summary>验证 Next 返回第一个允许的状态。</summary>
    [Fact]
    public void NextPicksFirstAllowedCandidate()
    {
        Assert.Equal(
            WorkflowState.WaitingValidation,
            WorkflowStateRules.Next(WorkflowState.Running, WorkflowState.WaitingValidation, WorkflowState.Failed));
    }
}
