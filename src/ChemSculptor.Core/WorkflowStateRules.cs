using ChemSculptor.Domain;

namespace ChemSculptor.Core;

public static class WorkflowStateRules
{
    private static readonly IReadOnlyDictionary<WorkflowState, WorkflowState[]> Transitions =
        new Dictionary<WorkflowState, WorkflowState[]>
        {
            [WorkflowState.Draft] = [WorkflowState.Ready, WorkflowState.Canceled],
            [WorkflowState.Ready] = [WorkflowState.Running, WorkflowState.Canceled, WorkflowState.Suspended],
            [WorkflowState.Running] =
            [
                WorkflowState.WaitingValidation,
                WorkflowState.Passed,
                WorkflowState.Failed,
                WorkflowState.Recovering,
                WorkflowState.AwaitingApproval,
                WorkflowState.Canceled,
                WorkflowState.Suspended
            ],
            [WorkflowState.WaitingValidation] =
            [
                WorkflowState.Passed,
                WorkflowState.Failed,
                WorkflowState.Recovering,
                WorkflowState.AwaitingApproval
            ],
            [WorkflowState.Recovering] =
                [WorkflowState.Running, WorkflowState.AwaitingApproval, WorkflowState.Failed],
            [WorkflowState.AwaitingApproval] =
                [WorkflowState.Running, WorkflowState.Recovering, WorkflowState.Canceled],
            [WorkflowState.Passed] = [WorkflowState.Archived],
            [WorkflowState.Canceled] = [],
            [WorkflowState.Suspended] = [WorkflowState.Ready, WorkflowState.Running, WorkflowState.Canceled],
            [WorkflowState.Archived] = []
        };

    public static bool CanTransition(WorkflowState from, WorkflowState to) =>
        Transitions.TryGetValue(from, out var allowed) && allowed.Contains(to);

    public static WorkflowState? Next(WorkflowState from, params WorkflowState[] candidates) =>
        candidates.FirstOrDefault(to => CanTransition(from, to));
}
