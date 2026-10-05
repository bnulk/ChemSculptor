namespace ChemSculptor.Anomaly.Models;

/// <summary>异常处理通用 Skill 标识。</summary>
public static class AnomalySkillIds
{
    /// <summary>检查波函数稳定性。</summary>
    public const string CheckWavefunctionStability =
        "anomaly.check-wavefunction-stability";

    /// <summary>生成波函数稳定性修正方案。</summary>
    public const string PlanWavefunctionStabilityCorrection =
        "anomaly.plan-wavefunction-stability-correction";
}
