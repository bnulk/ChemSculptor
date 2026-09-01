using ChemSculptor.Core;
using ChemSculptor.Domain;

namespace ChemSculptor.Core.Tests;

public class WorkflowStateRulesTests
{
    [Fact]
    public void RunningCanMoveToPassed()
    {
        Assert.True(WorkflowStateRules.CanTransition(WorkflowState.Running, WorkflowState.Passed));
    }

    [Fact]
    public void DraftCannotSkipToPassed()
    {
        Assert.False(WorkflowStateRules.CanTransition(WorkflowState.Draft, WorkflowState.Passed));
    }

    [Fact]
    public void NextPicksFirstAllowedCandidate()
    {
        Assert.Equal(
            WorkflowState.WaitingValidation,
            WorkflowStateRules.Next(WorkflowState.Running, WorkflowState.WaitingValidation, WorkflowState.Failed));
    }
}
