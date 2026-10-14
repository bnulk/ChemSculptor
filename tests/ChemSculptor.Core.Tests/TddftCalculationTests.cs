using ChemSculptor.Compute;
using ChemSculptor.Compute.Gaussian;
using ChemSculptor.Compute.Gaussian.Tddft;
using ChemSculptor.Conversation;

namespace ChemSculptor.Core.Tests;

/// <summary>TDDFT 单点计算测试。</summary>
public class TddftCalculationTests
{
    /// <summary>验证 Gaussian TDDFT route 被识别并转换为规范参数。</summary>
    [Fact]
    public async Task ResolvesTddftRouteParameters()
    {
        string text =
            "#p cam-b3lyp/6-31G* em=GD3BJ scfcyc=200 td(nstates=50) nosymm";
        RuleBasedTaskInterpreter interpreter =
            new RuleBasedTaskInterpreter();
        InterpretedTask intent =
            await interpreter.InterpretAsync(text);

        Assert.Equal(
            CalculationTaskType.TdDft,
            intent.TaskType);

        InMemoryConversationRepository repository =
            new InMemoryConversationRepository();
        CalculationParameterConversationService service =
            new CalculationParameterConversationService(
                repository,
                new RuleBasedCalculationParameterExtractor(),
                new CalculationParameterResolver());
        CalculationParameterResolutionContext context =
            new CalculationParameterResolutionContext();
        context.Engine = CalculationDefaults.DefaultProgram;
        context.TaskType = CalculationTaskType.TdDft;
        context.ElectronCount = 10;

        ParameterConversationTurn turn =
            await service.HandleAsync(
                "session-tddft",
                text,
                "O 0.0 0.0 0.0",
                CalculationDefaults.CreateDefaultTddftSinglePoint(),
                context);

        Assert.Equal(
            ParameterConversationTurnStatus.ReadyToSubmit,
            turn.Status);
        Assert.Equal(
            "50",
            turn.Resolution!.Spec.ExtraOptions[
                CalculationOptionKeys.TddftStateCount]);
        Assert.Equal(
            "Ignore",
            turn.Resolution.Spec.ExtraOptions[
                CalculationOptionKeys.Symmetry]);

        GaussianRouteKeywordBuildResult route =
            new GaussianRouteKeywordBuilder().Build(
                turn.Resolution.Spec);
        Assert.Contains("td(nstates=50)", route.RouteLine);
        Assert.Contains("em=GD3BJ", route.RouteLine);
        Assert.Contains("scfcyc=200", route.RouteLine);
        Assert.Contains("nosymm", route.RouteLine);
    }

    /// <summary>验证激发态和跃迁速度四极矩可以被解析。</summary>
    [Fact]
    public async Task ParsesExcitedStatesAndTransitionMoments()
    {
        string root = CreateTemporaryRoot();
        string outputPath = Path.Combine(root, "tddft.log");

        try
        {
            await File.WriteAllTextAsync(
                outputPath,
                " SCF Done:  E(RCAM-B3LYP) =  -76.300000     A.U. after    8 cycles\n" +
                " Excited State   1:      Singlet-?Sym    2.3061 eV  537.65 nm  f=0.0046  <S**2>=0.000\n" +
                "     417 -> 421       -0.48118\n" +
                "     418 -> 421        0.11434\n" +
                "     419 -> 421        0.48803\n" +
                " Ground to excited state transition velocity quadrupole moments (Au):\n" +
                "     state          XX          YY          ZZ          XY          XZ          YZ\n" +
                "         1       0.100000    0.200000    0.300000    0.010000    0.020000    0.030000\n" +
                " Normal termination of Gaussian 16\n");

            TddftCalculationResult result =
                await new GaussianTddftOutputParser()
                    .ParseTddftOutputAsync(outputPath);

            Assert.True(result.NormalTermination);
            Assert.Equal(-76.3, result.GroundStateEnergy);
            Assert.Single(result.ExcitedStates);
            TddftExcitedState state = result.ExcitedStates[0];
            Assert.Equal(1, state.StateIndex);
            Assert.Equal("Singlet-?Sym", state.StateLabel);
            Assert.Equal(2.3061, state.EnergyEv);
            Assert.Equal(537.65, state.WavelengthNm);
            Assert.Equal(0.0046, state.OscillatorStrength);
            Assert.Equal(3, state.Transitions.Count);
            Assert.Equal(417, state.Transitions[0].FromOrbital);
            Assert.Equal(421, state.Transitions[0].ToOrbital);
            Assert.Equal(-0.48118, state.Transitions[0].Coefficient);

            Assert.Single(
                result.TransitionVelocityQuadrupoleMoments);
            TddftTransitionMoment moment =
                result.TransitionVelocityQuadrupoleMoments[0];
            Assert.Equal(1, moment.StateIndex);
            Assert.Equal(6, moment.Components.Count);
            Assert.Equal("XX", moment.Components[0].Name);
            Assert.Equal(0.1, moment.Components[0].Value);
            Assert.Equal("YZ", moment.Components[5].Name);
            Assert.Equal(0.03, moment.Components[5].Value);
        }
        finally
        {
            DeleteTemporaryRoot(root);
        }
    }

    private static string CreateTemporaryRoot()
    {
        string root = Path.Combine(
            Path.GetTempPath(),
            "ChemSculptorTests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        return root;
    }

    private static void DeleteTemporaryRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, true);
        }
    }
}
