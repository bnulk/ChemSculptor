using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ChemSculptor.Anomaly.Models;
using ChemSculptor.Compute;
using ChemSculptor.FundamentalConstants.Chemistry.Elements;
using ChemSculptor.InputProcessor;
using ChemSculptor.ScientificData.Extraction.Abstractions;
using ChemSculptor.ScientificData.Extraction.Models;
using ChemSculptor.ScientificData.Models;

namespace ChemSculptor.ScientificData.Extraction;

/// <summary>把计算作业转换为计算点组成的科学成果。</summary>
public sealed class ScientificResultExtractor
    : IScientificResultExtractor
{
    private readonly IGeometryTextParser _geometryParser;

    /// <summary>创建科学成果提取器。</summary>
    public ScientificResultExtractor(
        IGeometryTextParser geometryParser)
    {
        if (geometryParser == null)
        {
            throw new ArgumentNullException(
                nameof(geometryParser));
        }

        _geometryParser = geometryParser;
    }

    /// <summary>提取科学成果。</summary>
    public async Task<ScientificResultExtractionResult> ExtractAsync(
        ScientificResultExtractionRequest request,
        CancellationToken cancellationToken = default)
    {
        ScientificResultExtractionResult extractionResult =
            new ScientificResultExtractionResult();

        if (request == null)
        {
            extractionResult.Error = "科学数据提取请求不能为空。";
            return extractionResult;
        }

        if (request.OriginalJob == null
            || string.IsNullOrWhiteSpace(
                request.OriginalJob.JobId))
        {
            extractionResult.Error = "原始计算作业不能为空。";
            return extractionResult;
        }

        MolecularGeometry molecularGeometry =
            await _geometryParser.ParseAsync(
                request.CoordinateText ?? string.Empty,
                cancellationToken);
        PointGeometry geometry = CreateGeometry(
            request.OriginalJob,
            molecularGeometry);

        CalculationJob? recoveryJob =
            request.RecoveryExecution == null
                ? null
                : request.RecoveryExecution.RecoveryJob;
        CalculationResult? recoveryResult =
            request.RecoveryExecution == null
                ? null
                : request.RecoveryExecution.Result;

        bool recoveryAccepted =
            recoveryJob != null
            && recoveryResult != null
            && recoveryResult.NormalTermination
            && request.RecoveryStabilityCheck != null
            && request.RecoveryStabilityCheck.Status
                == AnomalyCheckStatus.Passed;

        bool originalStable =
            request.OriginalStabilityCheck.Status
                == AnomalyCheckStatus.Passed
            || request.OriginalStabilityCheck.Status
                == AnomalyCheckStatus.Skipped;

        bool originalAccepted =
            !recoveryAccepted
            && request.OriginalResult != null
            && request.OriginalResult.NormalTermination
            && originalStable;

        CalculationPointStatus originalStatus =
            recoveryAccepted
                ? CalculationPointStatus.Superseded
                : originalAccepted
                    ? CalculationPointStatus.Accepted
                    : request.OriginalStabilityCheck.Status
                        == AnomalyCheckStatus.Finding
                        ? CalculationPointStatus.Rejected
                        : CalculationPointStatus.Candidate;

        string rootWorkflowId = ResolveRootWorkflowId(
            request,
            recoveryJob);
        CalculationResult originalResult =
            request.OriginalResult ?? new CalculationResult();
        int correctionCount = recoveryJob == null ? 0 : 1;
        string acceptedJobId = recoveryAccepted
            ? recoveryJob!.JobId
            : originalAccepted
                ? request.OriginalJob.JobId
                : string.Empty;

        PointProvenance originalProvenance =
            CreateOriginalProvenance(
                request.OriginalJob,
                rootWorkflowId,
                acceptedJobId,
                correctionCount,
                request.RecoveryExecution);

        CalculationPoint originalPoint =
            CreatePoint(
                request.OriginalJob,
                originalResult,
                geometry,
                molecularGeometry,
                originalStatus,
                originalProvenance,
                "原始计算点",
                BuildCanonicalStem(
                    molecularGeometry.Formula,
                    "original",
                    request.OriginalJob.Spec.Multiplicity));
        originalPoint.Validations.AddRange(
            CreateValidationRecords(
                request.OriginalValidationReport,
                request.OriginalStabilityCheck,
                request.OriginalJob.JobId,
                originalResult));

        List<CalculationPoint> points =
            new List<CalculationPoint>();
        points.Add(originalPoint);

        List<CalculationPointRelation> relations =
            new List<CalculationPointRelation>();

        CalculationPoint? recoveryPoint = null;

        if (recoveryJob != null)
        {
            CalculationPointStatus recoveryStatus =
                recoveryAccepted
                    ? CalculationPointStatus.Accepted
                    : CalculationPointStatus.Rejected;
            PointProvenance recoveryProvenance =
                CreateRecoveryProvenance(
                    request.OriginalJob,
                    recoveryJob,
                    originalPoint.Id,
                    acceptedJobId,
                    correctionCount);

            recoveryPoint = CreatePoint(
                recoveryJob,
                recoveryResult ?? new CalculationResult(),
                geometry,
                molecularGeometry,
                recoveryStatus,
                recoveryProvenance,
                "派生修正计算点",
                BuildCanonicalStem(
                    molecularGeometry.Formula,
                    "recovery",
                    recoveryJob.Spec.Multiplicity));
            recoveryPoint.Validations.AddRange(
                CreateValidationRecords(
                    new CalculationValidationReport(),
                    request.RecoveryStabilityCheck
                        ?? new AnomalyCheckResult(),
                    recoveryJob.JobId,
                    recoveryResult ?? new CalculationResult()));
            points.Add(recoveryPoint);

            CalculationPointRelation relation =
                new CalculationPointRelation();
            relation.Id =
                "relation-" +
                request.OriginalJob.JobId +
                "-to-" +
                recoveryJob.JobId;
            relation.Kind =
                CalculationPointRelationKind.DerivedFrom;
            relation.FromPointId = originalPoint.Id;
            relation.ToPointId = recoveryPoint.Id;
            relation.Label = "修正计算由原始计算派生";
            relation.Sequence = 1;
            relation.Description =
                request.CorrectionPlanning.Plan == null
                    ? string.Empty
                    : request.CorrectionPlanning.Plan
                        .Option == null
                        ? string.Empty
                        : request.CorrectionPlanning.Plan
                            .Option.Description;
            relations.Add(relation);
        }

        CalculationPoint? acceptedPoint = recoveryAccepted
            ? recoveryPoint
            : originalAccepted
                ? originalPoint
                : null;
        CalculationResult? acceptedResult = recoveryAccepted
            ? recoveryResult
            : originalAccepted
                ? request.OriginalResult
                : null;

        ScientificResult scientificResult =
            CreateScientificResult(
                request,
                rootWorkflowId,
                points,
                relations,
                acceptedPoint,
                acceptedResult,
                correctionCount);

        extractionResult.Succeeded = true;
        extractionResult.Result = scientificResult;
        return extractionResult;
    }

    private static ScientificResult CreateScientificResult(
        ScientificResultExtractionRequest request,
        string rootWorkflowId,
        List<CalculationPoint> points,
        List<CalculationPointRelation> relations,
        CalculationPoint? acceptedPoint,
        CalculationResult? acceptedResult,
        int correctionCount)
    {
        ScientificResult result = new ScientificResult();
        result.Id = "scientific-result-" + rootWorkflowId;
        result.Title =
            string.IsNullOrWhiteSpace(request.OriginalJob.Goal)
                ? "科学计算结果"
                : request.OriginalJob.Goal;
        result.Status = acceptedPoint == null
            ? ScientificResultStatus.Failed
            : ScientificResultStatus.Complete;

        string stabilitySummary =
            request.OriginalStabilityCheck.Status.ToString();

        if (request.RecoveryStabilityCheck != null)
        {
            stabilitySummary += " -> " +
                request.RecoveryStabilityCheck.Status.ToString();
        }

        result.Summary =
            "已记录 " +
            points.Count.ToString(CultureInfo.InvariantCulture) +
            " 个计算点；矫正 " +
            correctionCount.ToString(CultureInfo.InvariantCulture) +
            " 次；稳定性检查 " +
            stabilitySummary +
            "。";

        result.PointSet.Id = "point-set-" + rootWorkflowId;
        result.PointSet.Name = result.Title + " 点集";
        result.PointSet.Description =
            "由计算作业、验证和修正过程提取的科学计算点。";
        result.PointSet.Points.AddRange(points);
        result.PointSet.Relations.AddRange(relations);

        if (acceptedPoint != null
            && acceptedResult != null
            && acceptedResult.Energy.HasValue)
        {
            ScientificObservable observable =
                new ScientificObservable();
            observable.Id =
                "observable-" + rootWorkflowId + "-energy";
            observable.Name = "单点能量";
            observable.Kind =
                ScientificObservableKind.SinglePointEnergy;
            observable.NumericValue = acceptedResult.Energy.Value;
            observable.Unit = acceptedResult.EnergyUnit;
            observable.Formula =
                "采用已接受计算点的能量。";
            observable.PointIds.Add(acceptedPoint.Id);
            observable.Summary =
                "最终采用作业 " +
                acceptedPoint.Provenance.AcceptedJobId +
                " 的能量。";
            result.Observables.Add(observable);
        }

        result.Metadata["rootWorkflowId"] = rootWorkflowId;
        result.Metadata["correctionCount"] =
            correctionCount.ToString(
                CultureInfo.InvariantCulture);
        result.Metadata["acceptedPointId"] =
            acceptedPoint == null
                ? string.Empty
                : acceptedPoint.Id;
        result.Metadata["correctionPlanId"] =
            request.CorrectionPlanning.Plan == null
                ? string.Empty
                : request.CorrectionPlanning.Plan.Id;
        result.Metadata["anomalyRecordId"] =
            request.CorrectionPlanning.AnomalyRecordId;
        return result;
    }

    private static CalculationPoint CreatePoint(
        CalculationJob job,
        CalculationResult result,
        PointGeometry geometry,
        MolecularGeometry molecularGeometry,
        CalculationPointStatus status,
        PointProvenance provenance,
        string name,
        string canonicalStem)
    {
        CalculationPoint point = new CalculationPoint();
        point.Id = "point-" + job.JobId;
        point.Name = name;
        point.Kind = CalculationPointKind.Unknown;
        point.Status = status;
        point.CalculationJobId = job.JobId;
        point.Geometry = CloneGeometry(geometry);
        point.ElectronicState = CreateElectronicState(job.Spec);
        point.CalculationModel = CreateCalculationModel(job.Spec);
        point.ProgramData = CreateProgramData(job, result);
        point.Provenance = provenance;
        point.Artifacts.AddRange(
            CreateArtifactReferences(
                job,
                result,
                canonicalStem));
        point.Metadata["formula"] = molecularGeometry.Formula;
        point.Metadata["normalTermination"] =
            result.NormalTermination.ToString(
                CultureInfo.InvariantCulture);

        if (!string.IsNullOrWhiteSpace(molecularGeometry.Formula))
        {
            PointComponent component = new PointComponent();
            component.ComponentId = "component-1";
            component.Label = molecularGeometry.Formula;
            component.Charge = job.Spec.Charge;
            component.Multiplicity = job.Spec.Multiplicity;

            for (int index = 0;
                index < molecularGeometry.Atoms.Count;
                index++)
            {
                component.AtomIndices.Add(index);
            }

            point.Components.Add(component);
        }

        if (result.Energy.HasValue)
        {
            ScientificProperty energy =
                new ScientificProperty();
            energy.Name = "energy";
            energy.Kind = ScientificPropertyKind.Energy;
            energy.NumericValue = result.Energy.Value;
            energy.Unit = result.EnergyUnit;
            energy.Source = job.JobId;
            point.Properties.Add(energy);
        }

        return point;
    }

    private static PointGeometry CreateGeometry(
        CalculationJob job,
        MolecularGeometry molecularGeometry)
    {
        PointGeometry geometry = new PointGeometry();
        geometry.GeometryId = "geometry-" + job.JobId;
        geometry.SourceGeometryId = job.GeometryId;
        geometry.CoordinateText = molecularGeometry.RawText;
        geometry.CanonicalHash = ComputeHash(
            molecularGeometry.RawText);

        for (int index = 0;
            index < molecularGeometry.Atoms.Count;
            index++)
        {
            GeometryAtom source = molecularGeometry.Atoms[index];
            PointAtom atom = new PointAtom();
            atom.Index = index;
            atom.Element = source.Element;
            atom.X = source.X;
            atom.Y = source.Y;
            atom.Z = source.Z;

            ChemicalElement element;

            if (ElementCatalog.TryGetBySymbol(
                source.Element,
                out element))
            {
                atom.AtomicNumber = element.AtomicNumber;
            }

            geometry.Atoms.Add(atom);
        }

        return geometry;
    }

    private static PointGeometry CloneGeometry(
        PointGeometry source)
    {
        PointGeometry clone = new PointGeometry();
        clone.GeometryId = source.GeometryId;
        clone.SourceGeometryId = source.SourceGeometryId;
        clone.CoordinateText = source.CoordinateText;
        clone.CanonicalHash = source.CanonicalHash;

        for (int index = 0;
            index < source.Atoms.Count;
            index++)
        {
            PointAtom atom = source.Atoms[index];
            PointAtom atomClone = new PointAtom();
            atomClone.Index = atom.Index;
            atomClone.Element = atom.Element;
            atomClone.AtomicNumber = atom.AtomicNumber;
            atomClone.X = atom.X;
            atomClone.Y = atom.Y;
            atomClone.Z = atom.Z;
            clone.Atoms.Add(atomClone);
        }

        return clone;
    }

    private static PointElectronicState CreateElectronicState(
        CalculationSpec spec)
    {
        PointElectronicState state =
            new PointElectronicState();
        state.Kind = MapElectronicStateKind(
            spec.ElectronicStateObjective);
        state.Charge = spec.Charge;
        state.Multiplicity = spec.Multiplicity;
        state.StateLabel = spec.TargetStateLabel;
        return state;
    }

    private static PointCalculationModel CreateCalculationModel(
        CalculationSpec spec)
    {
        PointCalculationModel model =
            new PointCalculationModel();
        model.Program = spec.Program;
        model.ProgramVersion = string.Empty;
        model.Method = spec.Method;
        model.Basis = spec.Basis;
        model.Environment = spec.Solvent;

        foreach (KeyValuePair<string, string> pair in
            spec.ExtraOptions)
        {
            model.Parameters[pair.Key] = pair.Value;
        }

        return model;
    }

    private static PointProgramData CreateProgramData(
        CalculationJob job,
        CalculationResult result)
    {
        PointProgramData data = new PointProgramData();
        data.ProgramCode = NormalizeProgramCode(job.Spec.Program);
        data.ProgramVersion = string.Empty;

        if (!string.IsNullOrWhiteSpace(job.InputFilePath))
        {
            data.InputFormat = Path.GetExtension(
                job.InputFilePath).TrimStart('.');
        }

        foreach (KeyValuePair<string, string> pair in
            job.Spec.ExtraOptions)
        {
            data.Values[pair.Key] = pair.Value;
        }

        if (!string.IsNullOrWhiteSpace(result.Program))
        {
            data.Values["resultProgram"] = result.Program;
        }

        return data;
    }

    private static List<PointArtifactReference>
        CreateArtifactReferences(
            CalculationJob job,
            CalculationResult result,
            string canonicalStem)
    {
        List<PointArtifactReference> references =
            new List<PointArtifactReference>();

        if (result.Artifacts == null
            || result.Artifacts.Count == 0)
        {
            return references;
        }

        HashSet<string> usedFileNames =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        for (int index = 0;
            index < result.Artifacts.Count;
            index++)
        {
            CalculationArtifactDescriptor artifact =
                result.Artifacts[index];

            if (string.IsNullOrWhiteSpace(
                artifact.RelativePath))
            {
                continue;
            }

            string extension = Path.GetExtension(
                artifact.RelativePath);
            string fileStem = canonicalStem;
            string fileName = fileStem + extension;
            int duplicateIndex = 2;

            while (!usedFileNames.Add(fileName))
            {
                fileStem =
                    canonicalStem +
                    "-" +
                    duplicateIndex.ToString(
                        CultureInfo.InvariantCulture);
                fileName =
                    fileStem +
                    extension;
                duplicateIndex++;
            }

            PointArtifactReference reference =
                new PointArtifactReference();
            reference.ArtifactId =
                "artifact-" +
                job.JobId +
                "-" +
                index.ToString(
                    CultureInfo.InvariantCulture);
            reference.CalculationJobId = job.JobId;
            reference.Kind = MapArtifactKind(
                artifact.Kind);
            reference.RelativePath = artifact.RelativePath;
            reference.DownloadFileName = fileName;
            reference.CanonicalStem = fileStem;
            reference.CanonicalExtension = extension;
            reference.MediaType = artifact.MediaType;
            reference.Length = artifact.Length;
            reference.Sha256 = artifact.Sha256;
            reference.CanDownload = true;
            reference.CanUseForRestart =
                artifact.CanUseForRestart;
            references.Add(reference);
        }

        return references;
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

    private static string BuildCanonicalStem(
        string formula,
        string role,
        int multiplicity)
    {
        string basis = string.IsNullOrWhiteSpace(formula)
            ? "point"
            : formula;
        StringBuilder builder = new StringBuilder();

        for (int index = 0; index < basis.Length; index++)
        {
            char value = basis[index];

            if (char.IsLetterOrDigit(value)
                || value == '-'
                || value == '_'
                || value == '.')
            {
                builder.Append(value);
            }
            else
            {
                builder.Append('-');
            }
        }

        return builder.ToString() +
            "-" +
            role +
            "-m" +
            multiplicity.ToString(
                CultureInfo.InvariantCulture);
    }

    private static string NormalizeProgramCode(string program)
    {
        if (string.IsNullOrWhiteSpace(program))
        {
            return string.Empty;
        }

        StringBuilder builder = new StringBuilder();

        for (int index = 0; index < program.Length; index++)
        {
            char value = program[index];

            if (char.IsLetterOrDigit(value))
            {
                builder.Append(char.ToLowerInvariant(value));
            }
            else if (builder.Length > 0
                && builder[builder.Length - 1] != '-')
            {
                builder.Append('-');
            }
        }

        return builder.ToString().Trim('-');
    }

    private static PointElectronicStateKind MapElectronicStateKind(
        ElectronicStateObjective objective)
    {
        if (objective == ElectronicStateObjective.GroundState)
        {
            return PointElectronicStateKind.GroundState;
        }

        if (objective == ElectronicStateObjective.TargetSpinState)
        {
            return PointElectronicStateKind.SpinState;
        }

        if (objective == ElectronicStateObjective.TargetExcitedState)
        {
            return PointElectronicStateKind.ExcitedState;
        }

        return PointElectronicStateKind.Unknown;
    }

    private static PointProvenance CreateOriginalProvenance(
        CalculationJob originalJob,
        string rootWorkflowId,
        string acceptedJobId,
        int correctionCount,
        RecoveryJobExecutionResult? recoveryExecution)
    {
        PointProvenance provenance =
            new PointProvenance();
        provenance.InitialJobId = originalJob.JobId;
        provenance.AcceptedJobId = acceptedJobId;
        provenance.RootWorkflowId = rootWorkflowId;
        provenance.CorrectionCount = correctionCount;
        provenance.AcceptedBy = "ScientificResultExtractor";
        provenance.AcceptanceSummary = correctionCount > 0
            ? "原始点被修正结果取代。"
            : "原始点在通过必要验证后被接受。";
        return provenance;
    }

    private static PointProvenance CreateRecoveryProvenance(
        CalculationJob originalJob,
        CalculationJob recoveryJob,
        string parentPointId,
        string acceptedJobId,
        int correctionCount)
    {
        PointProvenance provenance =
            new PointProvenance();
        provenance.InitialJobId = originalJob.JobId;
        provenance.AcceptedJobId = acceptedJobId;
        provenance.RootWorkflowId =
            string.IsNullOrWhiteSpace(recoveryJob.RootWorkflowId)
                ? originalJob.JobId
                : recoveryJob.RootWorkflowId;
        provenance.ParentPointId = parentPointId;
        provenance.CorrectionCount = correctionCount;
        provenance.AcceptedBy = "ScientificResultExtractor";
        provenance.AcceptanceSummary =
            "派生修正在稳定性复检通过后被接受。";
        return provenance;
    }

    private static List<PointValidationRecord>
        CreateValidationRecords(
            CalculationValidationReport report,
            AnomalyCheckResult stabilityCheck,
            string jobId,
            CalculationResult result)
    {
        List<PointValidationRecord> records =
            new List<PointValidationRecord>();
        records.Add(CreateNormalTerminationRecord(
            jobId,
            result));

        if (report != null && report.Checks != null)
        {
            for (int index = 0;
                index < report.Checks.Count;
                index++)
            {
                CalculationValidationCheck check =
                    report.Checks[index];
                PointValidationRecord record =
                    new PointValidationRecord();
                record.Id =
                    "validation-" +
                    jobId +
                    "-" +
                    index.ToString(
                        CultureInfo.InvariantCulture);
                record.Code = check.Code;
                record.Status = check.Passed
                    ? PointValidationStatus.Passed
                    : PointValidationStatus.Finding;
                record.Summary =
                    string.IsNullOrWhiteSpace(check.Message)
                        ? check.Description
                        : check.Message;
                record.IsRequired =
                    check.Requirement
                    == CalculationValidationRequirement.Required;
                record.JobId = jobId;
                record.ValidatedAt = report.ValidatedAt;
                records.Add(record);
            }
        }

        if (stabilityCheck != null
            && (!string.IsNullOrWhiteSpace(
                    stabilityCheck.Code)
                || stabilityCheck.Status
                    != AnomalyCheckStatus.NotRun))
        {
            PointValidationRecord record =
                new PointValidationRecord();
            record.Id = "stability-" + jobId;
            record.Code = string.IsNullOrWhiteSpace(
                stabilityCheck.Code)
                ? "wavefunction-stability"
                : stabilityCheck.Code;
            record.Status = MapStabilityStatus(
                stabilityCheck.Status);
            record.Summary = stabilityCheck.Summary;
            record.IsRequired = stabilityCheck.IsRequired;
            record.JobId = jobId;
            record.ValidatedAt =
                stabilityCheck.CompletedAt
                ?? DateTimeOffset.UtcNow;
            records.Add(record);
        }

        return records;
    }

    private static PointValidationRecord
        CreateNormalTerminationRecord(
            string jobId,
            CalculationResult result)
    {
        PointValidationRecord record =
            new PointValidationRecord();
        record.Id = "normal-termination-" + jobId;
        record.Code = "calculation.normal-termination";
        record.Status = result.NormalTermination
            ? PointValidationStatus.Passed
            : PointValidationStatus.Finding;
        record.Summary = result.NormalTermination
            ? "计算程序正常结束。"
            : "计算程序没有正常结束。";
        record.IsRequired = true;
        record.JobId = jobId;
        return record;
    }

    private static PointValidationStatus MapStabilityStatus(
        AnomalyCheckStatus status)
    {
        if (status == AnomalyCheckStatus.Passed)
        {
            return PointValidationStatus.Passed;
        }

        if (status == AnomalyCheckStatus.Finding)
        {
            return PointValidationStatus.Finding;
        }

        if (status == AnomalyCheckStatus.Skipped)
        {
            return PointValidationStatus.Skipped;
        }

        if (status == AnomalyCheckStatus.Inconclusive)
        {
            return PointValidationStatus.Inconclusive;
        }

        if (status == AnomalyCheckStatus.ExecutionFailed
            || status == AnomalyCheckStatus.Canceled)
        {
            return PointValidationStatus.Failed;
        }

        return PointValidationStatus.NotRun;
    }

    private static string ResolveRootWorkflowId(
        ScientificResultExtractionRequest request,
        CalculationJob? recoveryJob)
    {
        if (!string.IsNullOrWhiteSpace(
            request.RootWorkflowId))
        {
            return request.RootWorkflowId;
        }

        if (recoveryJob != null
            && !string.IsNullOrWhiteSpace(
                recoveryJob.RootWorkflowId))
        {
            return recoveryJob.RootWorkflowId;
        }

        if (!string.IsNullOrWhiteSpace(
            request.OriginalJob.WorkflowId))
        {
            return request.OriginalJob.WorkflowId;
        }

        return request.OriginalJob.JobId;
    }

    private static string ComputeHash(string value)
    {
        byte[] input = Encoding.UTF8.GetBytes(
            value ?? string.Empty);
        byte[] hash = SHA256.HashData(input);
        return Convert.ToHexString(hash);
    }
}
