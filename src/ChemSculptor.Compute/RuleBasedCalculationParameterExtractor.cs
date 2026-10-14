using System.Globalization;
using System.Text.RegularExpressions;

namespace ChemSculptor.Compute;

/// <summary>基于冻结别名和规则的参数提取器。</summary>
public sealed class RuleBasedCalculationParameterExtractor
    : ICalculationParameterExtractor
{
    private const RegexOptions PatternOptions =
        RegexOptions.Compiled
        | RegexOptions.IgnoreCase
        | RegexOptions.CultureInvariant;

    private static readonly Regex UnknownRawKeywordPattern =
        new Regex(
            @"(?<![A-Za-z0-9])[A-Za-z][A-Za-z0-9_-]*\s*=\s*[^\s,，;；]+",
            PatternOptions);

    private readonly List<ExtractionRule> _rules;

    /// <summary>创建规则版参数提取器。</summary>
    public RuleBasedCalculationParameterExtractor()
    {
        _rules = CreateRules();
    }

    /// <summary>从用户文本提取参数意图。</summary>
    public Task<CalculationParameterExtractionResult> ExtractAsync(
        string text,
        CalculationParameterExtractionContext context,
        CancellationToken cancellationToken = default)
    {
        CalculationParameterExtractionResult result =
            new CalculationParameterExtractionResult();

        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(result);
        }

        List<PendingMatch> matches = new List<PendingMatch>();

        for (int ruleIndex = 0;
            ruleIndex < _rules.Count;
            ruleIndex++)
        {
            ExtractionRule rule = _rules[ruleIndex];

            if (!RuleAppliesToContext(rule, context))
            {
                continue;
            }

            MatchCollection ruleMatches =
                rule.Pattern.Matches(text);

            for (int matchIndex = 0;
                matchIndex < ruleMatches.Count;
                matchIndex++)
            {
                Match match = ruleMatches[matchIndex];

                if (!match.Success || match.Length == 0)
                {
                    continue;
                }

                PendingMatch pending = new PendingMatch();
                pending.Rule = rule;
                pending.Match = match;
                matches.Add(pending);
            }
        }

        matches.Sort(CompareMatches);

        List<PendingMatch> accepted = new List<PendingMatch>();

        for (int index = 0; index < matches.Count; index++)
        {
            PendingMatch candidate = matches[index];

            if (OverlapsAccepted(accepted, candidate))
            {
                continue;
            }

            accepted.Add(candidate);
        }

        for (int index = 0; index < accepted.Count; index++)
        {
            PendingMatch pending = accepted[index];
            ExtractionRule rule = pending.Rule;

            CalculationParameterIntent intent =
                new CalculationParameterIntent();
            intent.Key = rule.Key;
            intent.Value = rule.ValueFactory(pending.Match);
            intent.RawText = pending.Match.Value;
            intent.State = string.IsNullOrWhiteSpace(intent.Value)
                ? CalculationParameterIntentState.KeyOnly
                : CalculationParameterIntentState.ExplicitValue;
            intent.Source = ParameterSource.User;
            intent.Confidence = rule.Confidence;
            intent.StartIndex = pending.Match.Index;
            intent.EndIndex = pending.Match.Index + pending.Match.Length;
            result.Intents.Add(intent);
        }

        AddUnsupportedRawKeywords(
            text,
            accepted,
            result);
        result.Intents.Sort(CompareIntents);
        return Task.FromResult(result);
    }

    private static void AddUnsupportedRawKeywords(
        string text,
        List<PendingMatch> accepted,
        CalculationParameterExtractionResult result)
    {
        MatchCollection matches =
            UnknownRawKeywordPattern.Matches(text);

        for (int index = 0; index < matches.Count; index++)
        {
            Match match = matches[index];

            if (!match.Success
                || match.Length == 0
                || OverlapsAccepted(
                    accepted,
                    match.Index,
                    match.Length))
            {
                continue;
            }

            CalculationParameterIntent intent =
                new CalculationParameterIntent();
            intent.Key = string.Empty;
            intent.Value = match.Value;
            intent.RawText = match.Value;
            intent.State =
                CalculationParameterIntentState.Unsupported;
            intent.Source = ParameterSource.User;
            intent.Confidence = 0.5;
            intent.StartIndex = match.Index;
            intent.EndIndex = match.Index + match.Length;
            result.Intents.Add(intent);
        }
    }

    private static int CompareMatches(
        PendingMatch left,
        PendingMatch right)
    {
        int priority =
            right.Rule.Priority.CompareTo(left.Rule.Priority);

        if (priority != 0)
        {
            return priority;
        }

        int length =
            right.Match.Length.CompareTo(left.Match.Length);

        if (length != 0)
        {
            return length;
        }

        return left.Match.Index.CompareTo(right.Match.Index);
    }

    private static int CompareIntents(
        CalculationParameterIntent left,
        CalculationParameterIntent right)
    {
        return left.StartIndex.CompareTo(right.StartIndex);
    }

    private static bool OverlapsAccepted(
        List<PendingMatch> accepted,
        PendingMatch candidate)
    {
        int candidateStart = candidate.Match.Index;
        int candidateEnd =
            candidate.Match.Index + candidate.Match.Length;

        for (int index = 0; index < accepted.Count; index++)
        {
            int acceptedStart = accepted[index].Match.Index;
            int acceptedEnd =
                acceptedStart + accepted[index].Match.Length;

            if (candidateStart < acceptedEnd
                && acceptedStart < candidateEnd)
            {
                return true;
            }
        }

        return false;
    }

    private static bool OverlapsAccepted(
        List<PendingMatch> accepted,
        int start,
        int length)
    {
        int end = start + length;

        for (int index = 0; index < accepted.Count; index++)
        {
            int acceptedStart = accepted[index].Match.Index;
            int acceptedEnd =
                acceptedStart + accepted[index].Match.Length;

            if (start < acceptedEnd
                && acceptedStart < end)
            {
                return true;
            }
        }

        return false;
    }

    private static List<ExtractionRule> CreateRules()
    {
        List<ExtractionRule> rules = new List<ExtractionRule>();

        AddSingleValueAliasRules(
            rules,
            CalculationOptionKeys.Engine,
            "Gaussian 16");
        AddSingleValueAliasRules(
            rules,
            CalculationOptionKeys.Method,
            "CAM-B3LYP");
        AddSingleValueAliasRules(
            rules,
            CalculationOptionKeys.Basis,
            "6-31G*");

        AddWavefunctionStabilityCheckRules(rules);
        AddTddftRules(rules);
        AddRoutePrintLevelRules(rules);
        AddDispersionRules(rules);
        AddSymmetryRules(rules);
        AddScfIterationRules(rules);
        AddSolventRules(rules);
        AddChargeRules(rules);
        AddMultiplicityRules(rules);
        AddOptimizationRules(rules);

        return rules;
    }

    private static void AddWavefunctionStabilityCheckRules(
        List<ExtractionRule> rules)
    {
        AddRegexRule(
            rules,
            CalculationOptionKeys.WavefunctionStabilityCheck,
            @"(?:不|不做|不要|无需|不需要|不进行|不检查|跳过|忽略|取消)\s*波函数稳定性(?:检查)?",
            match => "Skip",
            1.0,
            5);
        AddRegexRule(
            rules,
            CalculationOptionKeys.WavefunctionStabilityCheck,
            @"波函数稳定性(?:检查)?",
            match => "Check",
            0.8,
            2);
    }

    private static void AddTddftRules(
        List<ExtractionRule> rules)
    {
        AddRegexRule(
            rules,
            CalculationOptionKeys.TddftStateCount,
            @"td\s*\(\s*nstates\s*=\s*(?<value>\d+)\s*\)",
            NormalizeIntegerValue,
            1.0,
            5);
        AddRegexRule(
            rules,
            CalculationOptionKeys.TddftStateCount,
            @"(?:nstates|TDDFT\s*态数|激发态数量|激发态数|态数)\s*(?:=|:|：|为|是)?\s*(?<value>\d+)",
            NormalizeIntegerValue,
            0.9,
            4);
    }

    private static bool RuleAppliesToContext(
        ExtractionRule rule,
        CalculationParameterExtractionContext context)
    {
        if (rule.TaskTypes.Count == 0)
        {
            return true;
        }

        for (int index = 0; index < rule.TaskTypes.Count; index++)
        {
            if (rule.TaskTypes[index] == context.TaskType)
            {
                return true;
            }
        }

        return false;
    }

    private static void AddRoutePrintLevelRules(
        List<ExtractionRule> rules)
    {
        AddAliasRule(rules, CalculationOptionKeys.RoutePrintLevel,
            "#p", "Verbose", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.RoutePrintLevel,
            "#n", "Normal", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.RoutePrintLevel,
            "#t", "Terse", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.RoutePrintLevel,
            "#", "Default", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.RoutePrintLevel,
            "详细输出", "Verbose", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.RoutePrintLevel,
            "普通输出", "Normal", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.RoutePrintLevel,
            "简短输出", "Terse", 1.0, 3);
    }

    private static void AddSingleValueAliasRules(
        List<ExtractionRule> rules,
        string key,
        string value)
    {
        CalculationParameterDefinition? definition =
            CalculationParameterCatalog.Find(key);

        if (definition == null)
        {
            return;
        }

        for (int index = 0;
            index < definition.Aliases.Count;
            index++)
        {
            AddAliasRule(
                rules,
                key,
                definition.Aliases[index],
                value,
                1.0,
                2);
        }
    }

    private static void AddDispersionRules(
        List<ExtractionRule> rules)
    {
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "em=GD3BJ", "GD3BJ", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "GD3BJ", "GD3BJ", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "gd3bj", "GD3BJ", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "D3BJ", "GD3BJ", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "d3bj", "GD3BJ", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "em=GD3", "GD3", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "GD3", "GD3", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "gd3", "GD3", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "D3", "GD3", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "d3", "GD3", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "关闭色散", "None", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "不加色散", "None", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "不考虑色散", "None", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "no dispersion", "None", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "none", "None", 1.0, 3);

        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "色散", string.Empty, 0.9, 1);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "色散校正", string.Empty, 0.9, 1);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "加色散", string.Empty, 0.9, 1);
        AddAliasRule(rules, CalculationOptionKeys.DispersionCorrection,
            "经验色散", string.Empty, 0.9, 1);
    }

    private static void AddSymmetryRules(
        List<ExtractionRule> rules)
    {
        AddAliasRule(rules, CalculationOptionKeys.Symmetry,
            "默认对称性", "Default", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Symmetry,
            "程序默认对称性", "Default", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Symmetry,
            "symm", "Use", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Symmetry,
            "symmetry", "Use", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Symmetry,
            "使用对称性", "Use", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Symmetry,
            "保持对称性", "Use", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Symmetry,
            "开启对称性", "Use", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Symmetry,
            "nosymm", "Ignore", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Symmetry,
            "no symmetry", "Ignore", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Symmetry,
            "不使用对称性", "Ignore", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Symmetry,
            "关闭对称性", "Ignore", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Symmetry,
            "不要对称化", "Ignore", 1.0, 3);

        AddAliasRule(rules, CalculationOptionKeys.Symmetry,
            "对称性", string.Empty, 0.8, 1);
    }

    private static void AddScfIterationRules(
        List<ExtractionRule> rules)
    {
        AddRegexRule(
            rules,
            CalculationOptionKeys.ScfIterationLimit,
            @"scfcyc\s*=\s*(?<value>\d+)",
            NormalizeIntegerValue,
            1.0,
            4);
        AddRegexRule(
            rules,
            CalculationOptionKeys.ScfIterationLimit,
            @"(?:SCF\s*(?:最多|最大|迭代上限)|最多迭代|SCF\s*cycles)\s*(?:=|:|：|为|是)?\s*(?<value>\d+)",
            NormalizeIntegerValue,
            1.0,
            4);

        AddAliasRule(rules, CalculationOptionKeys.ScfIterationLimit,
            "使用默认 SCF 上限", string.Empty, 0.9, 1);
        AddAliasRule(rules, CalculationOptionKeys.ScfIterationLimit,
            "默认 SCF 上限", string.Empty, 0.9, 1);
        AddAliasRule(rules, CalculationOptionKeys.ScfIterationLimit,
            "SCF", string.Empty, 0.7, 1);
    }

    private static void AddSolventRules(
        List<ExtractionRule> rules)
    {
        AddAliasRule(rules, CalculationOptionKeys.Solvent,
            "气相", "None", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Solvent,
            "无溶剂", "None", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Solvent,
            "不加溶剂", "None", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Solvent,
            "gas", "None", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Solvent,
            "no solvent", "None", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Solvent,
            "水溶剂", "Water", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Solvent,
            "SCRF=(Solvent=Water)", "Water", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Solvent,
            "SCRF=(Solvent=None)", "None", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Solvent,
            "水中", "Water", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Solvent,
            "水", "Water", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Solvent,
            "water", "Water", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Solvent,
            "aqueous", "Water", 1.0, 3);

        AddAliasRule(rules, CalculationOptionKeys.Solvent,
            "溶剂", string.Empty, 0.8, 1);
    }

    private static void AddChargeRules(
        List<ExtractionRule> rules)
    {
        AddRegexRule(
            rules,
            CalculationOptionKeys.Charge,
            @"(?:charge|总电荷|电荷)\s*(?:=|:|：)?\s*(?<value>[+-]?\d+)",
            NormalizeIntegerValue,
            1.0,
            4);

        AddAliasRule(rules, CalculationOptionKeys.Charge,
            "charge", string.Empty, 0.7, 1);
        AddAliasRule(rules, CalculationOptionKeys.Charge,
            "总电荷", string.Empty, 0.7, 1);
        AddAliasRule(rules, CalculationOptionKeys.Charge,
            "电荷", string.Empty, 0.7, 1);
    }

    private static void AddMultiplicityRules(
        List<ExtractionRule> rules)
    {
        AddRegexRule(
            rules,
            CalculationOptionKeys.Multiplicity,
            @"(?:multiplicity|自旋多重度|多重度)\s*(?:=|:|：)?\s*(?<value>\d+)",
            NormalizeIntegerValue,
            1.0,
            4);

        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "singlet", "1", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "单重态", "1", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "doublet", "2", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "二重态", "2", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "triplet", "3", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "三重态", "3", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "quartet", "4", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "四重态", "4", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "quintet", "5", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "五重态", "5", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "sextet", "6", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "六重态", "6", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "septet", "7", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "七重态", "7", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "octet", "8", 1.0, 3);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "八重态", "8", 1.0, 3);

        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "multiplicity", string.Empty, 0.7, 1);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "自旋多重度", string.Empty, 0.7, 1);
        AddAliasRule(rules, CalculationOptionKeys.Multiplicity,
            "多重度", string.Empty, 0.7, 1);
    }

    private static void AddOptimizationRules(
        List<ExtractionRule> rules)
    {
        AddOptimizationRegexRule(
            rules,
            CalculationOptionKeys.OptimizationMaxCycles,
            @"(?:opt\s*=\s*\(?\s*)?(?:maxcycles|maxcycle)\s*=\s*(?<value>\d+)",
            NormalizeIntegerValue,
            1.0,
            5);
        AddOptimizationRegexRule(
            rules,
            CalculationOptionKeys.OptimizationMaxCycles,
            @"(?:优化最大循环|最大优化循环|最大优化步数)\s*(?:=|:|：|为|是)?\s*(?<value>\d+)",
            NormalizeIntegerValue,
            1.0,
            5);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationMaxCycles,
            "最大优化循环",
            string.Empty,
            0.8,
            1);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationMaxCycles,
            "最大优化步数",
            string.Empty,
            0.8,
            1);

        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConvergence,
            "opt=tight",
            "Tight",
            1.0,
            5);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConvergence,
            "opt=verytight",
            "VeryTight",
            1.0,
            5);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConvergence,
            "tight",
            "Tight",
            1.0,
            3);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConvergence,
            "verytight",
            "VeryTight",
            1.0,
            3);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConvergence,
            "严格优化",
            "Tight",
            1.0,
            3);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConvergence,
            "非常严格优化",
            "VeryTight",
            1.0,
            3);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConvergence,
            "优化收敛",
            string.Empty,
            0.8,
            1);

        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationRestart,
            "opt=restart",
            "true",
            1.0,
            5);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationRestart,
            "restart",
            "true",
            1.0,
            3);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationRestart,
            "重启优化",
            "true",
            1.0,
            3);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationRestart,
            "继续优化",
            "true",
            1.0,
            3);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationRestart,
            "从检查点继续优化",
            "true",
            1.0,
            3);

        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationInitialHessian,
            "opt=CalcFC",
            "CalcFC",
            1.0,
            5);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationInitialHessian,
            "opt=ReadFC",
            "ReadFC",
            1.0,
            5);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationInitialHessian,
            "CalcFC",
            "CalcFC",
            1.0,
            3);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationInitialHessian,
            "ReadFC",
            "ReadFC",
            1.0,
            3);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationInitialHessian,
            "计算初始 Hessian",
            "CalcFC",
            1.0,
            3);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationInitialHessian,
            "读取初始 Hessian",
            "ReadFC",
            1.0,
            3);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationInitialHessian,
            "初始 Hessian",
            string.Empty,
            0.8,
            1);

        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConstraintMode,
            "冻结非氢原子",
            "HydrogensOnly",
            1.0,
            5);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConstraintMode,
            "固定非氢原子",
            "HydrogensOnly",
            1.0,
            5);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConstraintMode,
            "非氢原子冻结",
            "HydrogensOnly",
            1.0,
            5);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConstraintMode,
            "只优化氢原子",
            "HydrogensOnly",
            1.0,
            5);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConstraintMode,
            "仅优化氢原子",
            "HydrogensOnly",
            1.0,
            5);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConstraintMode,
            "只让氢原子移动",
            "HydrogensOnly",
            1.0,
            5);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConstraintMode,
            "freeze non-hydrogen",
            "HydrogensOnly",
            1.0,
            5);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConstraintMode,
            "freeze heavy atoms",
            "HydrogensOnly",
            1.0,
            5);
        AddOptimizationAliasRule(
            rules,
            CalculationOptionKeys.OptimizationConstraintMode,
            "hydrogens only",
            "HydrogensOnly",
            1.0,
            5);
    }

    private static void AddOptimizationAliasRule(
        List<ExtractionRule> rules,
        string key,
        string alias,
        string value,
        double confidence,
        int priority)
    {
        AddAliasRule(rules, key, alias, value, confidence, priority);
        rules[rules.Count - 1].TaskTypes.Add(
            CalculationTaskType.Optimization);
    }

    private static void AddOptimizationRegexRule(
        List<ExtractionRule> rules,
        string key,
        string pattern,
        Func<Match, string> valueFactory,
        double confidence,
        int priority)
    {
        AddRegexRule(
            rules,
            key,
            pattern,
            valueFactory,
            confidence,
            priority);
        rules[rules.Count - 1].TaskTypes.Add(
            CalculationTaskType.Optimization);
    }

    private static void AddAliasRule(
        List<ExtractionRule> rules,
        string key,
        string alias,
        string value,
        double confidence,
        int priority)
    {
        ExtractionRule rule = new ExtractionRule();
        rule.Key = key;
        rule.Pattern = new Regex(
            BuildAliasPattern(alias),
            PatternOptions);
        rule.ValueFactory = match => value;
        rule.Confidence = confidence;
        rule.Priority = priority;
        rules.Add(rule);
    }

    private static void AddRegexRule(
        List<ExtractionRule> rules,
        string key,
        string pattern,
        Func<Match, string> valueFactory,
        double confidence,
        int priority)
    {
        ExtractionRule rule = new ExtractionRule();
        rule.Key = key;
        rule.Pattern = new Regex(pattern, PatternOptions);
        rule.ValueFactory = valueFactory;
        rule.Confidence = confidence;
        rule.Priority = priority;
        rules.Add(rule);
    }

    private static string BuildAliasPattern(string alias)
    {
        string pattern = Regex.Escape(alias);

        if (StartsWithAsciiWordCharacter(alias))
        {
            pattern = "(?<![A-Za-z0-9])" + pattern;
        }

        if (EndsWithAsciiWordCharacter(alias))
        {
            pattern = pattern + "(?![A-Za-z0-9])";
        }

        return pattern;
    }

    private static bool StartsWithAsciiWordCharacter(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return IsAsciiWordCharacter(value[0]);
    }

    private static bool EndsWithAsciiWordCharacter(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return false;
        }

        return IsAsciiWordCharacter(value[value.Length - 1]);
    }

    private static bool IsAsciiWordCharacter(char value)
    {
        return (value >= 'A' && value <= 'Z')
            || (value >= 'a' && value <= 'z')
            || (value >= '0' && value <= '9');
    }

    private static string NormalizeIntegerValue(Match match)
    {
        Group group = match.Groups["value"];
        int value;

        if (!group.Success
            || !int.TryParse(
                group.Value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out value))
        {
            return string.Empty;
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }

    private sealed class ExtractionRule
    {
        public string Key { get; set; } = string.Empty;

        public Regex Pattern { get; set; } =
            new Regex("$^", PatternOptions);

        public Func<Match, string> ValueFactory { get; set; } =
            match => string.Empty;

        public double Confidence { get; set; }

        public int Priority { get; set; }

        public List<CalculationTaskType> TaskTypes { get; set; } =
            new List<CalculationTaskType>();
    }

    private sealed class PendingMatch
    {
        public ExtractionRule Rule { get; set; } =
            new ExtractionRule();

        public Match Match { get; set; } =
            Regex.Match(string.Empty, "$^");
    }
}
