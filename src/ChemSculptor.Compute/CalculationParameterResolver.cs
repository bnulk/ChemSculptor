using System.Globalization;

namespace ChemSculptor.Compute;

/// <summary>合并、校验和规范化计算参数。</summary>
public sealed class CalculationParameterResolver
{
    private const string DispersionRequestDefault = "GD3BJ";

    /// <summary>解析参数意图并写入计算方案。</summary>
    public CalculationParameterResolutionResult Resolve(
        CalculationSpec spec,
        IReadOnlyList<CalculationParameterIntent> intents,
        CalculationParameterResolutionContext context)
    {
        if (spec == null)
        {
            throw new ArgumentNullException(nameof(spec));
        }

        if (intents == null)
        {
            throw new ArgumentNullException(nameof(intents));
        }

        if (context == null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        CalculationParameterResolutionResult result =
            new CalculationParameterResolutionResult();
        result.Spec = spec;
        result.RequiresApproval = false;

        Dictionary<string, ParameterIntentGroup> groups =
            GroupIntents(intents, result);

        ApplyEngine(context, result);
        ApplyExplicitValues(groups, result);
        ApplyKeyOnlyValues(groups, result);
        ApplyGlobalDefaults(result);
        ResolveChargeAndMultiplicity(
            groups,
            context,
            result);
        ValidateValues(result);
        BuildAppliedParameters(groups, result);
        SetStatus(result);
        return result;
    }

    private static Dictionary<string, ParameterIntentGroup>
        GroupIntents(
            IReadOnlyList<CalculationParameterIntent> intents,
            CalculationParameterResolutionResult result)
    {
        Dictionary<string, ParameterIntentGroup> groups =
            new Dictionary<string, ParameterIntentGroup>(
                StringComparer.OrdinalIgnoreCase);

        for (int index = 0; index < intents.Count; index++)
        {
            CalculationParameterIntent intent = intents[index];

            if (intent == null
                || string.IsNullOrWhiteSpace(intent.Key))
            {
                AddDiagnostic(
                    result,
                    "parameter.unknown",
                    CalculationDiagnosticSeverity.Error,
                    string.Empty,
                    "无法识别参数。",
                    intent == null ? string.Empty : intent.RawText);
                continue;
            }

            if (intent.State
                == CalculationParameterIntentState.Unsupported)
            {
                AddDiagnostic(
                    result,
                    "parameter.unsupported",
                    CalculationDiagnosticSeverity.Error,
                    intent.Key,
                    "当前阶段不支持该参数。",
                    intent.RawText);
                continue;
            }

            ParameterIntentGroup? group;
            if (!groups.TryGetValue(intent.Key, out group)
                || group == null)
            {
                group = new ParameterIntentGroup();
                group.Key = intent.Key;
                groups[intent.Key] = group;
            }

            if (intent.State
                == CalculationParameterIntentState.KeyOnly)
            {
                group.KeyOnlyIntents.Add(intent);
            }
            else
            {
                group.ValueIntents.Add(intent);
            }
        }

        return groups;
    }

    private static void ApplyEngine(
        CalculationParameterResolutionContext context,
        CalculationParameterResolutionResult result)
    {
        if (string.IsNullOrWhiteSpace(context.Engine))
        {
            context.Engine = CalculationDefaults.DefaultProgram;
        }

        CalculationParameterDefinition? engineDefinition =
            CalculationParameterCatalog.Find(
                CalculationOptionKeys.Engine);

        if (engineDefinition == null
            || !ContainsValue(
                engineDefinition.AllowedValues,
                context.Engine))
        {
            AddDiagnostic(
                result,
                "parameter.unsupported",
                CalculationDiagnosticSeverity.Error,
                CalculationOptionKeys.Engine,
                "当前阶段只支持 Gaussian 16。",
                context.Engine);
            return;
        }

        result.Spec.Program = context.Engine;
        SetParameter(
            result.Spec,
            CalculationOptionKeys.Engine,
            context.Engine,
            ParameterSource.Default,
            "计算引擎默认为 Gaussian 16。");
    }

    private static void ApplyExplicitValues(
        Dictionary<string, ParameterIntentGroup> groups,
        CalculationParameterResolutionResult result)
    {
        foreach (KeyValuePair<string, ParameterIntentGroup> pair in
            groups)
        {
            ParameterIntentGroup group = pair.Value;

            if (group.ValueIntents.Count == 0)
            {
                continue;
            }

            List<string> values = GetDistinctValues(
                group.ValueIntents);

            if (values.Count == 0)
            {
                AddQuestion(
                    result,
                    "parameter.missing",
                    group.Key,
                    "请提供 " + group.Key + " 的值。",
                    CalculationParameterQuestionType.Value,
                    new List<string>(),
                    string.Empty);
                continue;
            }

            if (values.Count > 1)
            {
                AddQuestion(
                    result,
                    "parameter.conflict.same-key",
                    group.Key,
                    "参数 " + group.Key + " 存在多个不同值，请选择。",
                    CalculationParameterQuestionType.Choice,
                    values,
                    string.Empty);
                continue;
            }

            CalculationParameterIntent intent =
                GetHighestConfidenceIntent(group.ValueIntents);
            string value = values[0];

            if (string.Equals(
                group.Key,
                CalculationOptionKeys.Engine,
                StringComparison.OrdinalIgnoreCase)
                && !string.Equals(
                    value,
                    CalculationDefaults.DefaultProgram,
                    StringComparison.OrdinalIgnoreCase))
            {
                AddDiagnostic(
                    result,
                    "parameter.unsupported",
                    CalculationDiagnosticSeverity.Error,
                    group.Key,
                    "当前阶段只支持 Gaussian 16。",
                    intent.RawText);
                continue;
            }

            ApplyValue(
                result.Spec,
                group.Key,
                value,
                ParameterSource.User,
                intent,
                result);
        }
    }

    private static void ApplyKeyOnlyValues(
        Dictionary<string, ParameterIntentGroup> groups,
        CalculationParameterResolutionResult result)
    {
        foreach (KeyValuePair<string, ParameterIntentGroup> pair in
            groups)
        {
            ParameterIntentGroup group = pair.Value;

            if (group.ValueIntents.Count > 0
                || group.KeyOnlyIntents.Count == 0)
            {
                continue;
            }

            CalculationParameterIntent intent =
                group.KeyOnlyIntents[0];

            if (string.Equals(
                group.Key,
                CalculationOptionKeys.DispersionCorrection,
                StringComparison.OrdinalIgnoreCase))
            {
                ApplyValue(
                    result.Spec,
                    group.Key,
                    DispersionRequestDefault,
                    ParameterSource.Default,
                    intent,
                    result);
                AddDiagnostic(
                    result,
                    "parameter.defaulted",
                    CalculationDiagnosticSeverity.Info,
                    group.Key,
                    "用户请求色散但未指定类型，使用请求默认值 GD3BJ。",
                    intent.RawText);
                continue;
            }

            if (IsUseDefaultIntent(intent))
            {
                continue;
            }

            CalculationParameterDefinition? definition =
                CalculationParameterCatalog.Find(group.Key);
            List<string> options = definition == null
                ? new List<string>()
                : new List<string>(
                    definition.AllowedValues);

            AddQuestion(
                result,
                "parameter.missing",
                group.Key,
                "请提供 " + group.Key + " 的值。",
                options.Count > 0
                    ? CalculationParameterQuestionType.Choice
                    : CalculationParameterQuestionType.Value,
                options,
                string.Empty);
        }
    }

    private static void ApplyGlobalDefaults(
        CalculationParameterResolutionResult result)
    {
        CalculationSpec spec = result.Spec;

        if (string.IsNullOrWhiteSpace(spec.Program))
        {
            spec.Program = CalculationDefaults.DefaultProgram;
        }

        if (string.IsNullOrWhiteSpace(spec.Method))
        {
            spec.Method = CalculationDefaults.DefaultMethod;
        }

        if (string.IsNullOrWhiteSpace(spec.Basis))
        {
            spec.Basis = CalculationDefaults.DefaultBasis;
        }

        SetDefaultExtraOption(
            spec,
            CalculationOptionKeys.DispersionCorrection,
            "None");
        SetDefaultExtraOption(
            spec,
            CalculationOptionKeys.Symmetry,
            "Default");
        SetDefaultExtraOption(
            spec,
            CalculationOptionKeys.ScfIterationLimit,
            CalculationDefaults
                .DefaultScfIterationLimit
                .ToString(CultureInfo.InvariantCulture));

        if (string.IsNullOrWhiteSpace(spec.Solvent))
        {
            spec.Solvent = "None";
        }
    }

    private static void ValidateValues(
        CalculationParameterResolutionResult result)
    {
        IReadOnlyList<CalculationParameterDefinition> definitions =
            CalculationParameterCatalog.List();

        for (int index = 0; index < definitions.Count; index++)
        {
            CalculationParameterDefinition definition =
                definitions[index];
            string value = ReadValue(result.Spec, definition.Key);

            if (string.IsNullOrWhiteSpace(value)
                || HasQuestionForParameter(
                    result,
                    definition.Key))
            {
                continue;
            }

            if (definition.AllowedValues.Count > 0
                && !ContainsValue(
                    definition.AllowedValues,
                    value))
            {
                AddDiagnostic(
                    result,
                    "parameter.invalid.value",
                    CalculationDiagnosticSeverity.Error,
                    definition.Key,
                    "参数值不在允许列表中：" + value,
                    value);
            }
        }

        ValidateIntegerRange(
            result,
            CalculationOptionKeys.ScfIterationLimit,
            1,
            1000,
            500);
        ValidatePositiveInteger(
            result,
            CalculationOptionKeys.Multiplicity);
    }

    private static void ResolveChargeAndMultiplicity(
        Dictionary<string, ParameterIntentGroup> groups,
        CalculationParameterResolutionContext context,
        CalculationParameterResolutionResult result)
    {
        bool chargeExplicit = HasExplicitValue(
            groups,
            CalculationOptionKeys.Charge);
        bool multiplicityExplicit = HasExplicitValue(
            groups,
            CalculationOptionKeys.Multiplicity);

        if (!context.ElectronCount.HasValue)
        {
            if (!multiplicityExplicit)
            {
                AddQuestion(
                    result,
                    "parameter.missing",
                    CalculationOptionKeys.Multiplicity,
                    "无法确定总电子数，请提供自旋多重度。",
                    CalculationParameterQuestionType.Value,
                    new List<string>(),
                    string.Empty);
            }

            return;
        }

        int electronCount = context.ElectronCount.Value;

        if (!multiplicityExplicit)
        {
            int multiplicity =
                CalculationDefaults
                    .GetDefaultMultiplicityFromElectronCount(
                        electronCount);
            ApplyValue(
                result.Spec,
                CalculationOptionKeys.Multiplicity,
                multiplicity.ToString(
                    CultureInfo.InvariantCulture),
                ParameterSource.System,
                null,
                result);
            return;
        }

        int explicitMultiplicity = result.Spec.Multiplicity;
        bool parityMatches =
            (electronCount % 2 == 0
                && explicitMultiplicity % 2 == 1)
            || (electronCount % 2 == 1
                && explicitMultiplicity % 2 == 0);

        if (!parityMatches)
        {
            string code = chargeExplicit
                ? "parameter.conflict.charge-multiplicity"
                : "parameter.conflict.charge-multiplicity";
            string prompt = chargeExplicit
                ? "电荷和自旋多重度不一致，请确认需要修改的参数。"
                : "默认电荷与自旋多重度不一致，请提供正确电荷或修改多重度。";

            AddQuestion(
                result,
                code,
                chargeExplicit
                    ? CalculationOptionKeys.Multiplicity
                    : CalculationOptionKeys.Charge,
                prompt,
                CalculationParameterQuestionType.Value,
                new List<string>(),
                string.Empty);
        }
    }

    private static void BuildAppliedParameters(
        Dictionary<string, ParameterIntentGroup> groups,
        CalculationParameterResolutionResult result)
    {
        IReadOnlyList<CalculationParameterDefinition> definitions =
            CalculationParameterCatalog.List();

        for (int index = 0; index < definitions.Count; index++)
        {
            CalculationParameterDefinition definition =
                definitions[index];
            string value = ReadValue(result.Spec, definition.Key);

            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            ResolvedCalculationParameter parameter =
                new ResolvedCalculationParameter();
            parameter.Key = definition.Key;
            parameter.Value = value;
            parameter.RawText = GetRawText(
                groups,
                definition.Key);
            parameter.Source = GetParameterSource(
                result.Spec,
                definition.Key);
            parameter.Confidence = 1.0;
            parameter.RiskLevel = definition.RiskLevel;
            parameter.RequiresApproval = definition.RequiresApproval;
            parameter.TranslatedKeyword = string.Empty;
            result.AppliedParameters.Add(parameter);
        }
    }

    private static bool HasQuestionForParameter(
        CalculationParameterResolutionResult result,
        string key)
    {
        for (int index = 0; index < result.Questions.Count; index++)
        {
            if (string.Equals(
                result.Questions[index].ParameterKey,
                key,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string GetRawText(
        Dictionary<string, ParameterIntentGroup> groups,
        string key)
    {
        ParameterIntentGroup? group;

        if (!groups.TryGetValue(key, out group))
        {
            return string.Empty;
        }

        if (group.ValueIntents.Count > 0)
        {
            return GetHighestConfidenceIntent(
                group.ValueIntents).RawText;
        }

        if (group.KeyOnlyIntents.Count > 0)
        {
            return group.KeyOnlyIntents[0].RawText;
        }

        return string.Empty;
    }

    private static void SetStatus(
        CalculationParameterResolutionResult result)
    {
        for (int index = 0;
            index < result.Diagnostics.Count;
            index++)
        {
            if (result.Diagnostics[index].Severity
                == CalculationDiagnosticSeverity.Error)
            {
                result.Status =
                    CalculationParameterResolutionStatus.Rejected;
                return;
            }
        }

        if (result.Questions.Count > 0)
        {
            result.Status =
                CalculationParameterResolutionStatus
                    .NeedsClarification;
            return;
        }

        result.Status =
            CalculationParameterResolutionStatus.Resolved;
    }

    private static void ApplyValue(
        CalculationSpec spec,
        string key,
        string value,
        ParameterSource source,
        CalculationParameterIntent? intent,
        CalculationParameterResolutionResult result)
    {
        CalculationParameterDefinition? definition =
            CalculationParameterCatalog.Find(key);

        if (definition == null)
        {
            AddDiagnostic(
                result,
                "parameter.unknown",
                CalculationDiagnosticSeverity.Error,
                key,
                "无法识别参数：" + key,
                intent == null ? string.Empty : intent.RawText);
            return;
        }

        if (definition.AllowedValues.Count > 0
            && !ContainsValue(definition.AllowedValues, value))
        {
            AddDiagnostic(
                result,
                "parameter.invalid.value",
                CalculationDiagnosticSeverity.Error,
                key,
                "参数值不在允许列表中：" + value,
                intent == null ? string.Empty : intent.RawText);
            return;
        }

        if (string.Equals(
            key,
            CalculationOptionKeys.Engine,
            StringComparison.OrdinalIgnoreCase))
        {
            spec.Program = value;
        }
        else if (string.Equals(
            key,
            CalculationOptionKeys.Method,
            StringComparison.OrdinalIgnoreCase))
        {
            spec.Method = value;
        }
        else if (string.Equals(
            key,
            CalculationOptionKeys.Basis,
            StringComparison.OrdinalIgnoreCase))
        {
            spec.Basis = value;
        }
        else if (string.Equals(
            key,
            CalculationOptionKeys.Solvent,
            StringComparison.OrdinalIgnoreCase))
        {
            spec.Solvent = value;
        }
        else if (string.Equals(
            key,
            CalculationOptionKeys.Charge,
            StringComparison.OrdinalIgnoreCase))
        {
            int charge;

            if (!int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out charge))
            {
                AddDiagnostic(
                    result,
                    "parameter.invalid.value",
                    CalculationDiagnosticSeverity.Error,
                    key,
                    "电荷必须是整数。",
                    intent == null ? string.Empty : intent.RawText);
                return;
            }

            spec.Charge = charge;
        }
        else if (string.Equals(
            key,
            CalculationOptionKeys.Multiplicity,
            StringComparison.OrdinalIgnoreCase))
        {
            int multiplicity;

            if (!int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out multiplicity))
            {
                AddDiagnostic(
                    result,
                    "parameter.invalid.value",
                    CalculationDiagnosticSeverity.Error,
                    key,
                    "自旋多重度必须是整数。",
                    intent == null ? string.Empty : intent.RawText);
                return;
            }

            spec.Multiplicity = multiplicity;
        }
        else
        {
            spec.ExtraOptions[key] = value;
        }

        SetParameter(
            spec,
            key,
            value,
            source,
            intent == null ? string.Empty : intent.RawText);
    }

    private static void SetParameter(
        CalculationSpec spec,
        string key,
        string value,
        ParameterSource source,
        string description)
    {
        CalculationParameterDefinition? definition =
            CalculationParameterCatalog.Find(key);
        CalculationParameter? parameter = FindParameter(spec, key);

        if (parameter == null)
        {
            parameter = new CalculationParameter();
            parameter.Name = key;
            parameter.DisplayName =
                definition == null
                    ? key
                    : definition.DisplayName;
            parameter.DefaultValue =
                definition == null
                    ? string.Empty
                    : definition.DefaultValue;
            parameter.RiskLevel =
                definition == null
                    ? CalculationRiskLevel.Info
                    : definition.RiskLevel;
            parameter.RequiresApproval =
                definition != null
                && definition.RequiresApproval;
            spec.Parameters.Add(parameter);
        }

        parameter.CurrentValue = value;
        parameter.Source = source;
        parameter.Description = description;
    }

    private static CalculationParameter? FindParameter(
        CalculationSpec spec,
        string key)
    {
        for (int index = 0; index < spec.Parameters.Count; index++)
        {
            CalculationParameter parameter = spec.Parameters[index];

            if (string.Equals(
                parameter.Name,
                key,
                StringComparison.OrdinalIgnoreCase))
            {
                return parameter;
            }
        }

        return null;
    }

    private static void SetDefaultExtraOption(
        CalculationSpec spec,
        string key,
        string value)
    {
        if (!spec.ExtraOptions.ContainsKey(key)
            || string.IsNullOrWhiteSpace(spec.ExtraOptions[key]))
        {
            spec.ExtraOptions[key] = value;
        }
    }

    private static string ReadValue(
        CalculationSpec spec,
        string key)
    {
        if (string.Equals(
            key,
            CalculationOptionKeys.Engine,
            StringComparison.OrdinalIgnoreCase))
        {
            return spec.Program;
        }

        if (string.Equals(
            key,
            CalculationOptionKeys.Method,
            StringComparison.OrdinalIgnoreCase))
        {
            return spec.Method;
        }

        if (string.Equals(
            key,
            CalculationOptionKeys.Basis,
            StringComparison.OrdinalIgnoreCase))
        {
            return spec.Basis;
        }

        if (string.Equals(
            key,
            CalculationOptionKeys.Solvent,
            StringComparison.OrdinalIgnoreCase))
        {
            return spec.Solvent;
        }

        if (string.Equals(
            key,
            CalculationOptionKeys.Charge,
            StringComparison.OrdinalIgnoreCase))
        {
            return spec.Charge.ToString(
                CultureInfo.InvariantCulture);
        }

        if (string.Equals(
            key,
            CalculationOptionKeys.Multiplicity,
            StringComparison.OrdinalIgnoreCase))
        {
            if (spec.Multiplicity <= 0)
            {
                return string.Empty;
            }

            return spec.Multiplicity.ToString(
                CultureInfo.InvariantCulture);
        }

        string? value;
        if (spec.ExtraOptions.TryGetValue(key, out value))
        {
            return value;
        }

        return string.Empty;
    }

    private static ParameterSource GetParameterSource(
        CalculationSpec spec,
        string key)
    {
        CalculationParameter? parameter =
            FindParameter(spec, key);

        if (parameter == null)
        {
            return ParameterSource.Default;
        }

        return parameter.Source;
    }

    private static bool HasExplicitValue(
        Dictionary<string, ParameterIntentGroup> groups,
        string key)
    {
        ParameterIntentGroup? group;

        if (!groups.TryGetValue(key, out group))
        {
            return false;
        }

        return group.ValueIntents.Count > 0;
    }

    private static List<string> GetDistinctValues(
        List<CalculationParameterIntent> intents)
    {
        List<string> values = new List<string>();

        for (int index = 0; index < intents.Count; index++)
        {
            string value = intents[index].Value;

            if (string.IsNullOrWhiteSpace(value)
                || ContainsValue(values, value))
            {
                continue;
            }

            values.Add(value);
        }

        return values;
    }

    private static CalculationParameterIntent
        GetHighestConfidenceIntent(
            List<CalculationParameterIntent> intents)
    {
        CalculationParameterIntent selected = intents[0];

        for (int index = 1; index < intents.Count; index++)
        {
            if (intents[index].Confidence > selected.Confidence)
            {
                selected = intents[index];
            }
        }

        return selected;
    }

    private static bool IsUseDefaultIntent(
        CalculationParameterIntent intent)
    {
        return intent.RawText.IndexOf(
            "默认",
            StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool ContainsValue(
        IReadOnlyList<string> values,
        string value)
    {
        for (int index = 0; index < values.Count; index++)
        {
            if (string.Equals(
                values[index],
                value,
                StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateIntegerRange(
        CalculationParameterResolutionResult result,
        string key,
        int minimum,
        int maximum,
        int warningThreshold)
    {
        string value = ReadValue(result.Spec, key);
        int number;

        if (!int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out number))
        {
            AddDiagnostic(
                result,
                "parameter.invalid.value",
                CalculationDiagnosticSeverity.Error,
                key,
                "参数必须是整数。",
                value);
            return;
        }

        if (number < minimum || number > maximum)
        {
            AddDiagnostic(
                result,
                "parameter.invalid.range",
                CalculationDiagnosticSeverity.Error,
                key,
                "参数超出允许范围 " +
                minimum.ToString(CultureInfo.InvariantCulture) +
                " 到 " +
                maximum.ToString(CultureInfo.InvariantCulture) +
                "。",
                value);
            return;
        }

        if (number > warningThreshold)
        {
            AddDiagnostic(
                result,
                "parameter.warning.range",
                CalculationDiagnosticSeverity.Warning,
                key,
                "参数值高于常规范围，请确认计算成本。",
                value);
        }
    }

    private static void ValidatePositiveInteger(
        CalculationParameterResolutionResult result,
        string key)
    {
        string value = ReadValue(result.Spec, key);

        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        int number;

        if (!int.TryParse(
            value,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out number)
            || number <= 0)
        {
            AddDiagnostic(
                result,
                "parameter.invalid.value",
                CalculationDiagnosticSeverity.Error,
                key,
                "参数必须是正整数。",
                value);
        }
    }

    private static void AddQuestion(
        CalculationParameterResolutionResult result,
        string code,
        string key,
        string prompt,
        CalculationParameterQuestionType questionType,
        List<string> options,
        string defaultOption)
    {
        CalculationParameterQuestion question =
            new CalculationParameterQuestion();
        question.QuestionId = code + "." + key;
        question.ParameterKey = key;
        question.Prompt = prompt;
        question.QuestionType = questionType;
        question.Options = new List<string>(options);
        question.DefaultOption = defaultOption;
        question.Required = true;
        result.Questions.Add(question);
    }

    private static void AddDiagnostic(
        CalculationParameterResolutionResult result,
        string code,
        CalculationDiagnosticSeverity severity,
        string key,
        string message,
        string rawText)
    {
        CalculationParameterDiagnostic diagnostic =
            new CalculationParameterDiagnostic();
        diagnostic.Code = code;
        diagnostic.Severity = severity;
        diagnostic.ParameterKey = key;
        diagnostic.Message = message;
        diagnostic.RawText = rawText ?? string.Empty;
        result.Diagnostics.Add(diagnostic);
    }

    private sealed class ParameterIntentGroup
    {
        public string Key { get; set; } = string.Empty;

        public List<CalculationParameterIntent> ValueIntents
        {
            get;
            set;
        } = new List<CalculationParameterIntent>();

        public List<CalculationParameterIntent> KeyOnlyIntents
        {
            get;
            set;
        } = new List<CalculationParameterIntent>();
    }
}
