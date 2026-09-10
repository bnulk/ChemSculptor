using ChemSculptor.Domain;

namespace ChemSculptor.Core;

public static class WorkflowStateRules
{
    private static readonly Dictionary<WorkflowState, List<WorkflowState>> Transitions = CreateTransitions();

    public static bool CanTransition(WorkflowState from, WorkflowState to)
    {
        List<WorkflowState>? allowed;
        if (!Transitions.TryGetValue(from, out allowed))
        {
            return false;
        }

        return allowed.Contains(to);
    }

    public static WorkflowState? Next(WorkflowState from, params WorkflowState[] candidates)
    {
        for (int index = 0; index < candidates.Length; index++)
        {
            if (CanTransition(from, candidates[index]))
            {
                return candidates[index];
            }
        }

        return null;
    }

    private static Dictionary<WorkflowState, List<WorkflowState>> CreateTransitions()
    {
        Dictionary<WorkflowState, List<WorkflowState>> transitions =
            new Dictionary<WorkflowState, List<WorkflowState>>();

        AddRule(transitions, WorkflowState.Draft, WorkflowState.Ready, WorkflowState.Canceled);
        AddRule(
            transitions,
            WorkflowState.Ready,
            WorkflowState.Running,
            WorkflowState.Canceled,
            WorkflowState.Suspended);
        AddRule(
            transitions,
            WorkflowState.Running,
            WorkflowState.WaitingValidation,
            WorkflowState.Passed,
            WorkflowState.Failed,
            WorkflowState.Recovering,
            WorkflowState.AwaitingApproval,
            WorkflowState.Canceled,
            WorkflowState.Suspended);
        AddRule(
            transitions,
            WorkflowState.WaitingValidation,
            WorkflowState.Passed,
            WorkflowState.Failed,
            WorkflowState.Recovering,
            WorkflowState.AwaitingApproval);
        AddRule(
            transitions,
            WorkflowState.Recovering,
            WorkflowState.Running,
            WorkflowState.AwaitingApproval,
            WorkflowState.Failed);
        AddRule(
            transitions,
            WorkflowState.AwaitingApproval,
            WorkflowState.Running,
            WorkflowState.Recovering,
            WorkflowState.Canceled);
        AddRule(transitions, WorkflowState.Passed, WorkflowState.Archived);
        AddRule(transitions, WorkflowState.Canceled);
        AddRule(
            transitions,
            WorkflowState.Suspended,
            WorkflowState.Ready,
            WorkflowState.Running,
            WorkflowState.Canceled);
        AddRule(transitions, WorkflowState.Archived);

        return transitions;
    }

    private static void AddRule(
        Dictionary<WorkflowState, List<WorkflowState>> transitions,
        WorkflowState from,
        params WorkflowState[] allowedStates)
    {
        List<WorkflowState> allowed = new List<WorkflowState>();

        for (int index = 0; index < allowedStates.Length; index++)
        {
            allowed.Add(allowedStates[index]);
        }

        transitions.Add(from, allowed);
    }
}
