using ChemSculptor.Compute;
using ChemSculptor.FundamentalConstants.Chemistry.Elements;
using ChemSculptor.InputProcessor.GeometryIntake;

namespace ChemSculptor.Skills.Common.CalculationResultValidation;

/// <summary>验证几何优化结果的总体结构和数值。</summary>
public sealed class OptimizationResultValidator
    : ICalculationResultValidator
{
    /// <summary>验证器名称。</summary>
    public string Name
    {
        get { return "optimization-result-validator"; }
    }

    /// <summary>只处理包含优化结果的优化任务。</summary>
    public bool CanValidate(
        CalculationResultValidationRequest request)
    {
        if (request == null)
        {
            return false;
        }

        return (request.Job.Spec.TaskType
                == CalculationTaskType.Optimization
            || request.Job.Spec.TaskType
                == CalculationTaskType.OptimizationFrequency)
            && request.OptimizationResult != null;
    }

    /// <summary>验证优化总体结果。</summary>
    public CalculationValidationReport Validate(
        CalculationResultValidationRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        CalculationValidationReport report =
            new CalculationValidationReport();
        report.ValidatorName = Name;
        report.ValidatedAt = DateTimeOffset.UtcNow;

        CalculationJob job = request.Job;
        OptimizationResult result = request.OptimizationResult!;

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "optimization.job_id",
            "优化结果作业标识与作业一致",
            CalculationValidationScope.Structure,
            job.JobId,
            result.JobId,
            string.Equals(
                job.JobId,
                result.JobId,
                StringComparison.OrdinalIgnoreCase),
            "优化结果中的作业标识与请求不一致。");

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "optimization.program",
            "计算程序与方案一致",
            CalculationValidationScope.Structure,
            job.Spec.Program,
            result.Program,
            string.Equals(
                job.Spec.Program,
                result.Program,
                StringComparison.OrdinalIgnoreCase),
            "优化结果中的计算程序与方案不一致。");

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "optimization.normal_termination",
            "优化正常结束",
            CalculationValidationScope.ProgramOutput,
            "true",
            result.NormalTermination.ToString(),
            result.NormalTermination,
            "优化没有正常结束。");

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "optimization.completed",
            "几何优化已经完成",
            CalculationValidationScope.ScientificPlausibility,
            "true",
            NumericalCalculationValidationChecks
                .GetBooleanText(
                    result.OptimizationConverged),
            result.OptimizationConverged == true,
            "几何优化没有完成或收敛状态未知。");

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "optimization.failure_kind",
            "优化失败类别为 None",
            CalculationValidationScope.Structure,
            OptimizationFailureKind.None.ToString(),
            result.FailureKind.ToString(),
            result.FailureKind == OptimizationFailureKind.None,
            "优化被标记为失败：" +
            result.FailureKind.ToString());

        NumericalCalculationValidationChecks
            .AddEnergyPresentCheck(
                report,
                "optimization.final_energy_present",
                "存在最终能量",
                result.FinalEnergy);

        NumericalCalculationValidationChecks
            .AddEnergyFiniteCheck(
                report,
                "optimization.final_energy_finite",
                "最终能量是有限数值",
                result.FinalEnergy);

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "optimization.step_count",
            "步骤数量与轨迹长度一致",
            CalculationValidationScope.Structure,
            result.Trajectory.Count.ToString(),
            result.StepCount.ToString(),
            result.StepCount > 0
                && result.StepCount == result.Trajectory.Count,
            "优化步骤数量与轨迹长度不一致。");

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "optimization.final_geometry",
            "存在最终几何结构",
            CalculationValidationScope.Structure,
            "至少一个原子",
            result.FinalGeometry == null
                ? "缺失"
                : result.FinalGeometry.Atoms.Count.ToString(),
            result.FinalGeometry != null
                && result.FinalGeometry.Atoms.Count > 0,
            "优化结果中没有最终几何结构。");

        ValidateOptimizationConstraint(
            report,
            job.Spec,
            result);

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "optimization.output_file",
            "输出文件存在",
            CalculationValidationScope.ProgramOutput,
            "存在",
            result.OutputFilePath,
            !string.IsNullOrWhiteSpace(result.OutputFilePath)
                && File.Exists(result.OutputFilePath),
            "优化结果对应的输出文件不存在。");

        NumericalCalculationValidationChecks.SetReportStatus(
            report);
        return report;
    }

    private static void ValidateOptimizationConstraint(
        CalculationValidationReport report,
        CalculationSpec spec,
        OptimizationResult result)
    {
        string? mode;

        if (!spec.ExtraOptions.TryGetValue(
                CalculationOptionKeys.OptimizationConstraintMode,
                out mode)
            || !string.Equals(
                mode,
                "HydrogensOnly",
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (result.Trajectory.Count == 0
            || result.FinalGeometry == null
            || result.FinalGeometry.Atoms.Count == 0)
        {
            NumericalCalculationValidationChecks.AddRequiredCheck(
                report,
                "optimization.constraint.frozen_atoms",
                "非氢原子坐标保持不变",
                CalculationValidationScope.Structure,
                "可比较初始和最终几何",
                "缺失",
                false,
                "缺少轨迹或最终几何，无法验证冻结约束。");
            return;
        }

        CanonicalGeometry initial =
            result.Trajectory[0].Geometry;
        CanonicalGeometry finalGeometry =
            result.FinalGeometry;
        bool hydrogenFound = false;
        double maximumDisplacement = 0.0;

        for (int index = 0; index < initial.Atoms.Count; index++)
        {
            CanonicalAtom initialAtom = initial.Atoms[index];

            if (IsHydrogen(initialAtom.Element))
            {
                hydrogenFound = true;
                continue;
            }

            CanonicalAtom? finalAtom =
                FindAtom(finalGeometry, initialAtom.Index);

            if (finalAtom == null)
            {
                NumericalCalculationValidationChecks.AddRequiredCheck(
                    report,
                    "optimization.constraint.frozen_atoms",
                    "非氢原子坐标保持不变",
                    CalculationValidationScope.Structure,
                    "原子完整",
                    "缺原子 " + initialAtom.Index.ToString(),
                    false,
                    "最终几何缺少冻结原子。");
                return;
            }

            double dx = finalAtom.X - initialAtom.X;
            double dy = finalAtom.Y - initialAtom.Y;
            double dz = finalAtom.Z - initialAtom.Z;
            double displacement = Math.Sqrt(
                dx * dx + dy * dy + dz * dz);

            if (displacement > maximumDisplacement)
            {
                maximumDisplacement = displacement;
            }
        }

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "optimization.constraint.hydrogens_present",
            "体系中存在氢原子",
            CalculationValidationScope.Structure,
            "至少一个氢原子",
            hydrogenFound ? "存在" : "缺失",
            hydrogenFound,
            "冻结非氢原子的优化任务中没有找到氢原子。");

        NumericalCalculationValidationChecks.AddRequiredCheck(
            report,
            "optimization.constraint.frozen_atoms",
            "非氢原子坐标保持不变",
            CalculationValidationScope.Structure,
            "最大位移 <= 0.001 Angstrom",
            maximumDisplacement.ToString(
                "G6",
                System.Globalization.CultureInfo.InvariantCulture),
            maximumDisplacement <= 0.001,
            "冻结非氢原子的最大位移超过容差。");
    }

    private static CanonicalAtom? FindAtom(
        CanonicalGeometry geometry,
        int atomIndex)
    {
        for (int index = 0; index < geometry.Atoms.Count; index++)
        {
            if (geometry.Atoms[index].Index == atomIndex)
            {
                return geometry.Atoms[index];
            }
        }

        return null;
    }

    private static bool IsHydrogen(string element)
    {
        return string.Equals(
            element,
            "H",
            StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                element,
                "D",
                StringComparison.OrdinalIgnoreCase);
    }
}
