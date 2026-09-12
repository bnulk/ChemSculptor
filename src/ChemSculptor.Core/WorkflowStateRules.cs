using ChemSculptor.Domain;

namespace ChemSculptor.Core;

/// <summary>
/// 工作流状态转换规则。
/// 只负责判断“能否从状态 A 到状态 B”，不保存当前状态。
/// </summary>
public static class WorkflowStateRules
{
    private static readonly Dictionary<WorkflowState, List<WorkflowState>> Transitions = CreateTransitions();

    /// <summary>判断指定状态转换是否被允许。</summary>
    public static bool CanTransition(WorkflowState from, WorkflowState to)
    {
        List<WorkflowState>? allowed;
        if (!Transitions.TryGetValue(from, out allowed))
        {
            return false;
        }

        return allowed.Contains(to);
    }

    /// <summary>从候选状态中返回第一个允许到达的状态；都不允许时返回 null。</summary>
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

    /// <summary>构建完整的状态转换表。</summary>
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

    /// <summary>向转换表添加一条规则。</summary>
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
