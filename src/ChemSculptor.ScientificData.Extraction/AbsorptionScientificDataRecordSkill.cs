using System.Globalization;
using System.Text.Json;
using ChemSculptor.Compute;
using ChemSculptor.Domain;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;

namespace ChemSculptor.ScientificData.Extraction;

/// <summary>
/// 把吸收光谱工作流结果保存为科学成果。
/// </summary>
public sealed class AbsorptionScientificDataRecordSkill
    : ISkill,
      IAbsorptionSpectrumResultRecorder
{
    private const string ArchiveRequestKey = "archiveRequest";

    private static readonly JsonSerializerOptions JsonOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web);

    private readonly IScientificDataRepository _repository;
    private readonly List<string> _capabilities;

    /// <summary>创建吸收光谱科学数据记录 Skill。</summary>
    public AbsorptionScientificDataRecordSkill(
        IScientificDataRepository repository)
    {
        if (repository == null)
        {
            throw new ArgumentNullException(nameof(repository));
        }

        _repository = repository;
        _capabilities = new List<string>();
        _capabilities.Add(
            ScientificDataSkillIds.RecordAbsorptionSpectrum);
        _capabilities.Add("science.data");
    }

    /// <summary>技能名称。</summary>
    public string Name
    {
        get
        {
            return ScientificDataSkillIds
                .RecordAbsorptionSpectrum;
        }
    }

    /// <summary>技能版本。</summary>
    public string Version
    {
        get { return "1.0.0"; }
    }

    /// <summary>技能能力。</summary>
    public IReadOnlyList<string> Capabilities
    {
        get { return _capabilities; }
    }

    /// <summary>保存吸收光谱科学成果。</summary>
    public async Task<AbsorptionSpectrumArchiveResult> RecordAsync(
        AbsorptionSpectrumArchiveRequest request,
        CancellationToken cancellationToken = default)
    {
        AbsorptionSpectrumArchiveResult archiveResult =
            new AbsorptionSpectrumArchiveResult();

        if (request == null
            || request.Request == null
            || request.Result == null
            || request.Result.Tddft == null
            || request.Result.Tddft.Job == null
            || request.Result.Tddft.Result == null)
        {
            archiveResult.Error =
                "吸收光谱归档请求缺少 TDDFT 结果。";
            return archiveResult;
        }

        ScientificResult result = CreateScientificResult(
            request,
            request.Result.Tddft.Job,
            request.Result.Tddft.Result);
        await _repository.SaveAsync(
            result,
            cancellationToken);

        archiveResult.Succeeded = true;
        archiveResult.ScientificResultId = result.Id;
        return archiveResult;
    }

    /// <summary>执行 Skill 入口。</summary>
    public async Task<TaskResult> ExecuteAsync(
        TaskRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        string? json;

        if (!request.Inputs.TryGetValue(
                ArchiveRequestKey,
                out json)
            || string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException(
                "吸收光谱科学数据节点缺少 archiveRequest 输入。");
        }

        AbsorptionSpectrumArchiveRequest archiveRequest =
            JsonSerializer.Deserialize<
                AbsorptionSpectrumArchiveRequest>(
                    json,
                    JsonOptions)
            ?? new AbsorptionSpectrumArchiveRequest();
        AbsorptionSpectrumArchiveResult archiveResult =
            await RecordAsync(
                archiveRequest,
                cancellationToken);

        TaskResult taskResult = new TaskResult();
        taskResult.WorkflowId = request.WorkflowId;
        taskResult.NodeId = request.NodeId;
        taskResult.Succeeded = archiveResult.Succeeded;
        taskResult.Output = JsonSerializer.Serialize(
            archiveResult,
            JsonOptions);

        if (!archiveResult.Succeeded)
        {
            taskResult.Diagnostics = archiveResult.Error;
        }

        return taskResult;
    }

    /// <summary>当前 Skill 始终可用。</summary>
    public Task<bool> HealthAsync(
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true);
    }

    private static ScientificResult CreateScientificResult(
        AbsorptionSpectrumArchiveRequest archiveRequest,
        CalculationJob tddftJob,
        TddftCalculationResult tddftResult)
    {
        ScientificResult result = new ScientificResult();
        result.Id = "absorption-spectrum-" + tddftJob.JobId;
        result.Title = string.IsNullOrWhiteSpace(
                archiveRequest.Request.Goal)
            ? "吸收光谱结果"
            : archiveRequest.Request.Goal;
        result.Status =
            tddftResult.NormalTermination
            && tddftResult.ExcitedStates.Count > 0
                ? ScientificResultStatus.Complete
                : ScientificResultStatus.Partial;

        TddftExcitedState? strongest =
            FindStrongestState(tddftResult);
        result.Summary = BuildSummary(
            archiveRequest.Result.Mode,
            tddftResult,
            strongest);

        CalculationPoint point = CreateCalculationPoint(
            archiveRequest,
            tddftJob,
            tddftResult,
            strongest);
        result.PointSet.Id = "point-set-" + tddftJob.JobId;
        result.PointSet.Name = result.Title + " 点集";
        result.PointSet.Points.Add(point);

        if (tddftResult.GroundStateEnergy.HasValue)
        {
            ScientificObservable energy =
                new ScientificObservable();
            energy.Id =
                "observable-" + tddftJob.JobId + "-ground-energy";
            energy.Name = "TDDFT 基态能量";
            energy.Kind =
                ScientificObservableKind.SinglePointEnergy;
            energy.NumericValue =
                tddftResult.GroundStateEnergy.Value;
            energy.Unit = "Hartree";
            energy.Formula = "Gaussian SCF Done 能量。";
            energy.PointIds.Add(point.Id);
            energy.Summary =
                "TDDFT 单点计算报告的基态能量。";
            result.Observables.Add(energy);
        }

        ScientificObservable spectrum =
            new ScientificObservable();
        spectrum.Id =
            "observable-" + tddftJob.JobId + "-spectrum";
        spectrum.Name = "TDDFT 吸收光谱";
        spectrum.Kind = ScientificObservableKind.Spectrum;
        spectrum.Unit = "nm";
        spectrum.Formula =
            "Gaussian TDDFT 激发态波长和振子强度。";
        spectrum.PointIds.Add(point.Id);
        spectrum.Summary = strongest == null
            ? "未解析到激发态。"
            : "最强吸收态 " +
                strongest.StateIndex.ToString(
                    CultureInfo.InvariantCulture) +
                "，波长 " +
                strongest.WavelengthNm.ToString(
                    "G10",
                    CultureInfo.InvariantCulture) +
                " nm。";
        spectrum.TextValue = JsonSerializer.Serialize(
            tddftResult.ExcitedStates,
            JsonOptions);
        spectrum.Metadata["excitedStateCount"] =
            tddftResult.ExcitedStates.Count.ToString(
                CultureInfo.InvariantCulture);

        if (strongest != null)
        {
            spectrum.NumericValue = strongest.WavelengthNm;
            spectrum.Metadata["strongestStateIndex"] =
                strongest.StateIndex.ToString(
                    CultureInfo.InvariantCulture);
            spectrum.Metadata["strongestOscillatorStrength"] =
                strongest.OscillatorStrength.ToString(
                    "G10",
                    CultureInfo.InvariantCulture);
        }

        result.Observables.Add(spectrum);
        FillMetadata(
            result,
            archiveRequest,
            tddftJob,
            tddftResult,
            strongest);
        result.CompletedAt = DateTimeOffset.UtcNow;
        return result;
    }

    private static CalculationPoint CreateCalculationPoint(
        AbsorptionSpectrumArchiveRequest archiveRequest,
        CalculationJob tddftJob,
        TddftCalculationResult tddftResult,
        TddftExcitedState? strongest)
    {
        CalculationPoint point = new CalculationPoint();
        point.Id = "point-" + tddftJob.JobId;
        point.Name = "TDDFT 吸收光谱计算点";
        point.Kind = CalculationPointKind.Unknown;
        point.Status = tddftResult.NormalTermination
            && tddftResult.ExcitedStates.Count > 0
                ? CalculationPointStatus.Accepted
                : CalculationPointStatus.Rejected;
        point.CalculationJobId = tddftJob.JobId;
        point.Geometry.CoordinateText =
            ResolveCoordinateText(archiveRequest);
        point.CalculationModel.Program =
            tddftResult.Program;
        point.CalculationModel.Method =
            tddftResult.Method;
        point.CalculationModel.Basis =
            tddftResult.Basis;
        point.CalculationModel.Parameters[
            CalculationOptionKeys.TddftStateCount] =
            ReadTddftStateCount(archiveRequest.Request.TddftSpec);
        point.ElectronicState.Kind =
            PointElectronicStateKind.GroundState;
        point.ElectronicState.Multiplicity =
            tddftJob.Spec.Multiplicity;

        if (tddftResult.GroundStateEnergy.HasValue)
        {
            ScientificProperty energy =
                new ScientificProperty();
            energy.Name = "ground-state-energy";
            energy.Kind = ScientificPropertyKind.Energy;
            energy.NumericValue =
                tddftResult.GroundStateEnergy.Value;
            energy.Unit = "Hartree";
            energy.Source = tddftJob.JobId;
            point.Properties.Add(energy);
        }

        ScientificProperty stateCount =
            new ScientificProperty();
        stateCount.Name = "excited-state-count";
        stateCount.Kind = ScientificPropertyKind.Number;
        stateCount.NumericValue =
            tddftResult.ExcitedStates.Count;
        stateCount.Unit = "states";
        stateCount.Source = tddftJob.JobId;
        point.Properties.Add(stateCount);

        if (strongest != null)
        {
            ScientificProperty wavelength =
                new ScientificProperty();
            wavelength.Name = "strongest-absorption-wavelength";
            wavelength.Kind = ScientificPropertyKind.Array;
            wavelength.NumericValue = strongest.WavelengthNm;
            wavelength.Unit = "nm";
            wavelength.Source = tddftJob.JobId;
            point.Properties.Add(wavelength);
        }

        point.Provenance.InitialJobId = tddftJob.JobId;
        point.Provenance.AcceptedJobId = tddftJob.JobId;
        point.Provenance.RootWorkflowId =
            archiveRequest.Request.SessionId;
        point.Provenance.AcceptedBy =
            "AbsorptionScientificDataRecordSkill";
        point.Provenance.AcceptanceSummary =
            "TDDFT 正常结束并解析到激发态。";
        point.Metadata["absorptionMode"] =
            archiveRequest.Result.Mode.ToString();
        point.Metadata["outputFilePath"] =
            tddftResult.OutputFilePath;

        if (archiveRequest.Result.Optimization != null
            && archiveRequest.Result.Optimization.Result != null)
        {
            point.Kind = CalculationPointKind.Minimum;
            point.Metadata["optimizationJobId"] =
                archiveRequest.Result.Optimization.FinalJobId;
            point.Metadata["optimizationConverged"] =
                archiveRequest.Result.Optimization.Result
                    .OptimizationConverged
                    .GetValueOrDefault()
                    .ToString();
        }

        for (int index = 0;
            index < tddftResult.Artifacts.Count;
            index++)
        {
            CalculationArtifactDescriptor artifact =
                tddftResult.Artifacts[index];
            PointArtifactReference reference =
                new PointArtifactReference();
            reference.ArtifactId =
                "artifact-" +
                tddftJob.JobId +
                "-" +
                index.ToString(CultureInfo.InvariantCulture);
            reference.CalculationJobId = tddftJob.JobId;
            reference.Kind = MapArtifactKind(artifact.Kind);
            reference.RelativePath = artifact.RelativePath;
            reference.DownloadFileName = artifact.FileName;
            reference.CanonicalStem = Path.GetFileNameWithoutExtension(
                artifact.FileName);
            reference.CanonicalExtension =
                Path.GetExtension(artifact.FileName);
            reference.MediaType = artifact.MediaType;
            reference.Length = artifact.Length;
            reference.Sha256 = artifact.Sha256;
            reference.CanUseForRestart =
                artifact.CanUseForRestart;
            point.Artifacts.Add(reference);
        }

        return point;
    }

    private static void FillMetadata(
        ScientificResult result,
        AbsorptionSpectrumArchiveRequest archiveRequest,
        CalculationJob tddftJob,
        TddftCalculationResult tddftResult,
        TddftExcitedState? strongest)
    {
        result.Metadata["workflowId"] = "absorption-spectrum";
        result.Metadata["mode"] =
            archiveRequest.Result.Mode.ToString();
        result.Metadata["tddftJobId"] = tddftJob.JobId;
        result.Metadata["program"] = tddftResult.Program;
        result.Metadata["method"] = tddftResult.Method;
        result.Metadata["basis"] = tddftResult.Basis;
        result.Metadata["normalTermination"] =
            tddftResult.NormalTermination.ToString();
        result.Metadata["excitedStateCount"] =
            tddftResult.ExcitedStates.Count.ToString(
                CultureInfo.InvariantCulture);

        if (archiveRequest.Result.Optimization != null)
        {
            result.Metadata["optimizationJobId"] =
                archiveRequest.Result.Optimization.FinalJobId;
        }

        if (tddftResult.GroundStateEnergy.HasValue)
        {
            result.Metadata["groundStateEnergy"] =
                tddftResult.GroundStateEnergy.Value.ToString(
                    "G17",
                    CultureInfo.InvariantCulture);
        }

        if (strongest != null)
        {
            result.Metadata["strongestStateIndex"] =
                strongest.StateIndex.ToString(
                    CultureInfo.InvariantCulture);
            result.Metadata["strongestWavelengthNm"] =
                strongest.WavelengthNm.ToString(
                    "G10",
                    CultureInfo.InvariantCulture);
            result.Metadata["strongestOscillatorStrength"] =
                strongest.OscillatorStrength.ToString(
                    "G10",
                    CultureInfo.InvariantCulture);
        }
    }

    private static string ResolveCoordinateText(
        AbsorptionSpectrumArchiveRequest archiveRequest)
    {
        if (archiveRequest.Result.Optimization != null
            && archiveRequest.Result.Optimization.Result != null)
        {
            string text = FormatGeometry(
                archiveRequest.Result.Optimization.Result
                    .FinalGeometry);

            if (!string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
        }

        return archiveRequest.Request.CoordinateText;
    }

    private static string FormatGeometry(
        ChemSculptor.InputProcessor.GeometryIntake.CanonicalGeometry?
            geometry)
    {
        if (geometry == null || geometry.Atoms.Count == 0)
        {
            return string.Empty;
        }

        string text = string.Empty;

        for (int index = 0;
            index < geometry.Atoms.Count;
            index++)
        {
            ChemSculptor.InputProcessor.GeometryIntake.CanonicalAtom
                atom = geometry.Atoms[index];

            if (index > 0)
            {
                text = text + Environment.NewLine;
            }

            text = text +
                atom.Element +
                " " +
                atom.X.ToString(
                    "F6",
                    CultureInfo.InvariantCulture) +
                " " +
                atom.Y.ToString(
                    "F6",
                    CultureInfo.InvariantCulture) +
                " " +
                atom.Z.ToString(
                    "F6",
                    CultureInfo.InvariantCulture);
        }

        return text;
    }

    private static string ReadTddftStateCount(
        CalculationSpec? spec)
    {
        if (spec == null)
        {
            return CalculationDefaults.DefaultTddftStateCount
                .ToString(CultureInfo.InvariantCulture);
        }

        string? value;

        if (spec.ExtraOptions.TryGetValue(
                CalculationOptionKeys.TddftStateCount,
                out value)
            && !string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        return CalculationDefaults.DefaultTddftStateCount
            .ToString(CultureInfo.InvariantCulture);
    }

    private static TddftExcitedState? FindStrongestState(
        TddftCalculationResult result)
    {
        if (result.ExcitedStates.Count == 0)
        {
            return null;
        }

        TddftExcitedState strongest =
            result.ExcitedStates[0];

        for (int index = 1;
            index < result.ExcitedStates.Count;
            index++)
        {
            if (result.ExcitedStates[index]
                    .OscillatorStrength
                > strongest.OscillatorStrength)
            {
                strongest = result.ExcitedStates[index];
            }
        }

        return strongest;
    }

    private static string BuildSummary(
        AbsorptionSpectrumMode mode,
        TddftCalculationResult tddftResult,
        TddftExcitedState? strongest)
    {
        string modeText = mode == AbsorptionSpectrumMode.TddftOnly
            ? "直接 TDDFT"
            : mode == AbsorptionSpectrumMode
                .ConstrainedOptimizationThenTddft
                ? "冻结非氢原子后 TDDFT"
                : "优化后 TDDFT";

        if (strongest == null)
        {
            return modeText + " 未解析到激发态。";
        }

        return modeText +
            "，解析 " +
            tddftResult.ExcitedStates.Count.ToString(
                CultureInfo.InvariantCulture) +
            " 个激发态，最强吸收态 " +
            strongest.StateIndex.ToString(
                CultureInfo.InvariantCulture) +
            "，波长 " +
            strongest.WavelengthNm.ToString(
                "G10",
                CultureInfo.InvariantCulture) +
            " nm。";
    }

    private static ScientificArtifactKind MapArtifactKind(
        CalculationArtifactKind kind)
    {
        if (kind == CalculationArtifactKind.Input)
        {
            return ScientificArtifactKind.Input;
        }

        if (kind == CalculationArtifactKind.PrimaryOutput)
        {
            return ScientificArtifactKind.PrimaryOutput;
        }

        if (kind == CalculationArtifactKind.SupportingOutput)
        {
            return ScientificArtifactKind.SupportingOutput;
        }

        if (kind == CalculationArtifactKind.RestartState)
        {
            return ScientificArtifactKind.RestartState;
        }

        return ScientificArtifactKind.Other;
    }
}
