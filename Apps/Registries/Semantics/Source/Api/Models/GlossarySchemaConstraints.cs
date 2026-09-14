namespace Adr.Semantics.Models
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;

    internal static class GlossarySchemaConstraints
    {
        private static readonly HashSet<string> SupportedConstraints =
            new(StringComparer.Ordinal)
            {
                "enum",
                "format",
                "maximum",
                "minimum",
                "exclusiveMaximum",
                "exclusiveMinimum",
                "maxLength",
                "minLength",
                "multipleOf",
                "pattern",
            };

        public static GlossarySchemaConstraintResult Parse(string source, string schemaType)
        {
            var constraints = new Dictionary<string, object>(StringComparer.Ordinal);
            var issues = new List<GlossarySchemaConstraintIssue>();

            if (string.IsNullOrWhiteSpace(source))
            {
                return new GlossarySchemaConstraintResult(constraints, issues);
            }

            JsonDocument document;
            try
            {
                document = JsonDocument.Parse(source);
            }
            catch (JsonException exception)
            {
                issues.Add(new GlossarySchemaConstraintIssue("<document>", exception.Message));
                return new GlossarySchemaConstraintResult(constraints, issues);
            }

            using (document)
            {
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    issues.Add(
                        new GlossarySchemaConstraintIssue(
                            "<document>",
                            "the value must be a JSON object"
                        )
                    );
                    return new GlossarySchemaConstraintResult(constraints, issues);
                }

                var properties = document.RootElement.EnumerateObject().ToList();
                var duplicateNames = properties
                    .GroupBy(property => property.Name, StringComparer.Ordinal)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key)
                    .ToHashSet(StringComparer.Ordinal);

                foreach (var duplicateName in duplicateNames)
                {
                    issues.Add(
                        new GlossarySchemaConstraintIssue(
                            duplicateName,
                            "the JSON object contains the property more than once"
                        )
                    );
                }

                foreach (var property in properties.Where(property =>
                    !duplicateNames.Contains(property.Name)
                ))
                {
                    var reason = Validate(property.Name, property.Value, schemaType);
                    if (reason is not null)
                    {
                        issues.Add(new GlossarySchemaConstraintIssue(property.Name, reason));
                        continue;
                    }

                    constraints[property.Name] = property.Value.Clone();
                }
            }

            ValidateRelationships(constraints, issues);
            return new GlossarySchemaConstraintResult(constraints, issues);
        }

        private static string? Validate(string name, JsonElement value, string schemaType)
        {
            if (!SupportedConstraints.Contains(name))
            {
                return "the property is not a supported OpenAPI scalar constraint";
            }

            if (!IsAppropriateForType(name, schemaType))
            {
                return $"the property is not applicable to schema type '{schemaType}'";
            }

            return name switch
            {
                "enum" => ValidateEnum(value, schemaType),
                "format" =>
                    value.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(value.GetString())
                        ? null
                        : "the value must be a non-empty string",
                "maximum" or "minimum" => IsNumber(value)
                    ? null
                    : "the value must be a finite number",
                "exclusiveMaximum" or "exclusiveMinimum" =>
                    value.ValueKind is JsonValueKind.True or JsonValueKind.False
                        ? null
                        : "the value must be a boolean in OpenAPI 3.0",
                "maxLength" or "minLength" => IsNonNegativeInteger(value)
                    ? null
                    : "the value must be a non-negative integer",
                "multipleOf" => IsPositiveNumber(value)
                    ? null
                    : "the value must be a finite number greater than zero",
                "pattern" => ValidatePattern(value),
                _ => throw new InvalidOperationException($"Unhandled schema constraint '{name}'."),
            };
        }

        private static bool IsAppropriateForType(string name, string schemaType)
        {
            return name switch
            {
                "enum" => true,
                "format" => schemaType is "string" or "number" or "integer",
                "maximum"
                or "minimum"
                or "exclusiveMaximum"
                or "exclusiveMinimum"
                or "multipleOf" => schemaType is "number" or "integer",
                "maxLength" or "minLength" or "pattern" => schemaType == "string",
                _ => false,
            };
        }

        private static string? ValidateEnum(JsonElement value, string schemaType)
        {
            if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
            {
                return "the value must be a non-empty array";
            }

            if (!value.EnumerateArray().All(item => MatchesSchemaType(item, schemaType)))
            {
                return $"every enum value must match schema type '{schemaType}'";
            }

            var values = value.EnumerateArray().ToList();
            var distinctCount = schemaType switch
            {
                "string" => values
                    .Select(item => item.GetString())
                    .Distinct(StringComparer.Ordinal)
                    .Count(),
                "number" or "integer" => values.Select(item => item.GetDouble()).Distinct().Count(),
                "boolean" => values.Select(item => item.GetBoolean()).Distinct().Count(),
                _ => 0,
            };

            return distinctCount == values.Count ? null : "enum values must be unique";
        }

        private static string? ValidatePattern(JsonElement value)
        {
            if (value.ValueKind != JsonValueKind.String)
            {
                return "the value must be a string";
            }

            try
            {
                _ = new Regex(
                    value.GetString()!,
                    RegexOptions.CultureInvariant | RegexOptions.ECMAScript
                );
                return null;
            }
            catch (ArgumentException exception)
            {
                return $"the value is not a valid ECMA-262 regular expression: {exception.Message}";
            }
        }

        private static bool MatchesSchemaType(JsonElement value, string schemaType)
        {
            return schemaType switch
            {
                "string" => value.ValueKind == JsonValueKind.String,
                "number" => IsNumber(value),
                "integer" => IsInteger(value),
                "boolean" => value.ValueKind is JsonValueKind.True or JsonValueKind.False,
                _ => false,
            };
        }

        private static bool IsNumber(JsonElement value)
        {
            return value.ValueKind == JsonValueKind.Number
                && double.TryParse(
                    value.GetRawText(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var number
                )
                && double.IsFinite(number);
        }

        private static bool IsInteger(JsonElement value)
        {
            return IsNumber(value)
                && double.TryParse(
                    value.GetRawText(),
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out var number
                )
                && number == Math.Truncate(number);
        }

        private static bool IsNonNegativeInteger(JsonElement value)
        {
            return value.ValueKind == JsonValueKind.Number
                && value.TryGetInt64(out var number)
                && number >= 0;
        }

        private static bool IsPositiveNumber(JsonElement value)
        {
            return IsNumber(value) && value.GetDouble() > 0;
        }

        private static void ValidateRelationships(
            Dictionary<string, object> constraints,
            List<GlossarySchemaConstraintIssue> issues
        )
        {
            if (
                TryGetNumber(constraints, "minimum", out var minimum)
                && TryGetNumber(constraints, "maximum", out var maximum)
                && minimum > maximum
            )
            {
                constraints.Remove("maximum");
                issues.Add(
                    new GlossarySchemaConstraintIssue(
                        "maximum",
                        "the value must be greater than or equal to minimum"
                    )
                );
            }

            if (
                TryGetInteger(constraints, "minLength", out var minLength)
                && TryGetInteger(constraints, "maxLength", out var maxLength)
                && minLength > maxLength
            )
            {
                constraints.Remove("maxLength");
                issues.Add(
                    new GlossarySchemaConstraintIssue(
                        "maxLength",
                        "the value must be greater than or equal to minLength"
                    )
                );
            }

            ValidateExclusiveBound(
                constraints,
                issues,
                "exclusiveMinimum",
                "minimum"
            );
            ValidateExclusiveBound(
                constraints,
                issues,
                "exclusiveMaximum",
                "maximum"
            );

            if (
                TryGetNumber(constraints, "minimum", out minimum)
                && TryGetNumber(constraints, "maximum", out maximum)
                && minimum == maximum
            )
            {
                RemoveTrueExclusiveConstraint(constraints, issues, "exclusiveMinimum");
                RemoveTrueExclusiveConstraint(constraints, issues, "exclusiveMaximum");
            }
        }

        private static void ValidateExclusiveBound(
            Dictionary<string, object> constraints,
            List<GlossarySchemaConstraintIssue> issues,
            string exclusiveName,
            string boundName
        )
        {
            if (constraints.ContainsKey(exclusiveName) && !constraints.ContainsKey(boundName))
            {
                constraints.Remove(exclusiveName);
                issues.Add(
                    new GlossarySchemaConstraintIssue(
                        exclusiveName,
                        $"the property requires {boundName}"
                    )
                );
            }
        }

        private static void RemoveTrueExclusiveConstraint(
            Dictionary<string, object> constraints,
            List<GlossarySchemaConstraintIssue> issues,
            string name
        )
        {
            if (
                constraints.TryGetValue(name, out var value)
                && value is JsonElement element
                && element.ValueKind == JsonValueKind.True
            )
            {
                constraints.Remove(name);
                issues.Add(
                    new GlossarySchemaConstraintIssue(
                        name,
                        "the property would exclude the only value allowed by equal bounds"
                    )
                );
            }
        }

        private static bool TryGetNumber(
            Dictionary<string, object> constraints,
            string name,
            out double value
        )
        {
            if (constraints.TryGetValue(name, out var item) && item is JsonElement element)
            {
                value = element.GetDouble();
                return true;
            }

            value = default;
            return false;
        }

        private static bool TryGetInteger(
            Dictionary<string, object> constraints,
            string name,
            out long value
        )
        {
            if (constraints.TryGetValue(name, out var item) && item is JsonElement element)
            {
                value = element.GetInt64();
                return true;
            }

            value = default;
            return false;
        }
    }

    internal sealed record GlossarySchemaConstraintResult(
        IDictionary<string, object> Constraints,
        IReadOnlyList<GlossarySchemaConstraintIssue> Issues
    );

    internal sealed record GlossarySchemaConstraintIssue(string ConstraintName, string Reason);
}
