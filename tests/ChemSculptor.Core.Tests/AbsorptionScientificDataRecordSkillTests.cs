using ChemSculptor.Compute;
using ChemSculptor.ScientificData.Extraction;
using ChemSculptor.ScientificData.Models;
using ChemSculptor.ScientificData.Storage;

namespace ChemSculptor.Core.Tests;

/// <summary>吸收光谱科学数据归档测试。</summary>
public class AbsorptionScientificDataRecordSkillTests
{
    /// <summary>验证 TDDFT 结果会形成可检索的吸收光谱科学成果。</summary>
    [Fact]
    public async Task RecordsAbsorptionSpectrumScientificResult()
    {
        RecordingRepository repository =
            new RecordingRepository();
        AbsorptionScientificDataRecordSkill skill =
            new AbsorptionScientificDataRecordSkill(repository);

        CalculationJob tddftJob = new CalculationJob();
        tddftJob.JobId = "job-absorption-archive";
        tddftJob.Spec =
            CalculationDefaults.CreateDefaultTddftSinglePoint();
        tddftJob.Spec.Multiplicity = 1;

        TddftCalculationResult tddft =
            new TddftCalculationResult();
        tddft.JobId = tddftJob.JobId;
        tddft.NormalTermination = true;
        tddft.GroundStateEnergy = -76.3;
        TddftExcitedState state = new TddftExcitedState();
        state.StateIndex = 1;
        state.WavelengthNm = 300.0;
        state.OscillatorStrength = 0.5;
        tddft.ExcitedStates.Add(state);

        AbsorptionSpectrumArchiveRequest request =
            new AbsorptionSpectrumArchiveRequest();
        request.Request.Goal = "吸收光谱";
        request.Request.CoordinateText =
            "O 0.000000 0.000000 0.117300";
        request.Request.TddftSpec = tddftJob.Spec;
        request.Result.Succeeded = true;
        request.Result.Mode =
            AbsorptionSpectrumMode.TddftOnly;
        request.Result.Tddft = new TddftSubmissionResult();
        request.Result.Tddft.Job = tddftJob;
        request.Result.Tddft.Result = tddft;

        AbsorptionSpectrumArchiveResult result =
            await skill.RecordAsync(request);

        Assert.True(result.Succeeded);
        Assert.NotNull(repository.LastResult);
        ScientificResult scientific = repository.LastResult!;
        Assert.Equal(
            "absorption-spectrum-job-absorption-archive",
            scientific.Id);
        Assert.Single(scientific.PointSet.Points);
        Assert.Equal(
            "1",
            scientific.Metadata["excitedStateCount"]);
        Assert.Equal(
            "300",
            scientific.Metadata["strongestWavelengthNm"]);
        Assert.Contains(
            scientific.Observables,
            observable =>
                observable.Kind
                    == ScientificObservableKind.Spectrum);
    }

    private sealed class RecordingRepository
        : IScientificDataRepository
    {
        public ScientificResult? LastResult
        {
            get;
            private set;
        }

        public Task SaveAsync(
            ScientificResult result,
            CancellationToken cancellationToken = default)
        {
            LastResult = result;
            return Task.CompletedTask;
        }

        public Task<ScientificResult?> GetAsync(
            string resultId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                LastResult != null
                && LastResult.Id == resultId
                    ? LastResult
                    : null);
        }

        public IReadOnlyList<ScientificResult> List()
        {
            List<ScientificResult> results =
                new List<ScientificResult>();

            if (LastResult != null)
            {
                results.Add(LastResult);
            }

            return results;
        }
    }
}
