namespace ChemSculptor.Compute;

/// <summary>用户希望研究的电子态目标。</summary>
public enum ElectronicStateObjective
{
    /// <summary>尚未确定。</summary>
    Unknown,

    /// <summary>寻找当前方法下的最低能量稳定电子态。</summary>
    GroundState,

    /// <summary>固定某个自旋多重度。</summary>
    TargetSpinState,

    /// <summary>研究指定激发态。</summary>
    TargetExcitedState
}
