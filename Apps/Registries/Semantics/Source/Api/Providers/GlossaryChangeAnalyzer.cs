namespace Adr.Semantics.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using Adr.Semantics.Models;

    internal static class GlossaryChangeAnalyzer
    {
        public static GlossaryChangeKind Analyze(
            IEnumerable<GlossaryModel> previousTerms,
            IEnumerable<GlossaryModel> importedTerms,
            IReadOnlySet<Guid>? explicitlyBreakingTermIds = null
        )
        {
            var previous = previousTerms.ToList();
            var imported = importedTerms.ToList();
            var previousById = previous.ToDictionary(ParseId);
            var importedById = imported.ToDictionary(ParseId);
            var previousByName = previous.ToDictionary(
                item => item.Name,
                StringComparer.OrdinalIgnoreCase
            );

            if (previousById.Keys.Except(importedById.Keys).Any())
            {
                return GlossaryChangeKind.Major;
            }

            foreach (var term in imported)
            {
                var id = ParseId(term);
                if (
                    previousByName.TryGetValue(term.Name, out var previousWithName)
                    && ParseId(previousWithName) != id
                )
                {
                    return GlossaryChangeKind.Major;
                }

                if (
                    previousById.TryGetValue(id, out var previousWithId)
                    && !string.Equals(
                        previousWithId.Name,
                        term.Name,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    return GlossaryChangeKind.Major;
                }
            }

            var addedIds = importedById.Keys.Except(previousById.Keys).ToList();
            if (addedIds.Any(id => explicitlyBreakingTermIds?.Contains(id) == true))
            {
                return GlossaryChangeKind.Major;
            }

            var result = addedIds.Count > 0
                ? GlossaryChangeKind.Minor
                : GlossaryChangeKind.None;

            foreach (var id in importedById.Keys.Intersect(previousById.Keys))
            {
                var before = previousById[id];
                var after = importedById[id];
                if (
                    string.Equals(
                        GlossaryTermPersistence.GetContentHash(before),
                        GlossaryTermPersistence.GetContentHash(after),
                        StringComparison.Ordinal
                    )
                )
                {
                    continue;
                }

                if (explicitlyBreakingTermIds?.Contains(id) == true)
                {
                    return GlossaryChangeKind.Major;
                }

                if (IsMoreRestrictive(before, after))
                {
                    return GlossaryChangeKind.Major;
                }

                result = (GlossaryChangeKind)Math.Max(
                    (int)result,
                    (int)GlossaryChangeKind.Patch
                );
            }

            return result;
        }

        private static Guid ParseId(GlossaryModel term)
        {
            return Guid.Parse(term.StaticId);
        }

        private static bool IsMoreRestrictive(GlossaryModel before, GlossaryModel after)
        {
            if (IsPublished(before) && !IsPublished(after))
            {
                return true;
            }

            if (
                !string.Equals(before.SchemaType, after.SchemaType, StringComparison.Ordinal)
                && !(before.SchemaType == "integer" && after.SchemaType == "number")
            )
            {
                return true;
            }

            foreach (var constraint in after.SchemaConstraints)
            {
                if (!before.SchemaConstraints.TryGetValue(constraint.Key, out var oldValue))
                {
                    if (!IsIneffectiveExclusiveFlag(constraint.Key, constraint.Value))
                    {
                        return true;
                    }

                    continue;
                }

                if (JsonEquals(oldValue, constraint.Value))
                {
                    continue;
                }

                if (ChangeIsMoreRestrictive(constraint.Key, oldValue, constraint.Value))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPublished(GlossaryModel term)
        {
            return term.VerifiedDefinitionFlag && term.PublishToDevHub;
        }

        private static bool ChangeIsMoreRestrictive(string name, object before, object after)
        {
            return name switch
            {
                "minimum" or "minLength" => GetDecimal(after) > GetDecimal(before),
                "maximum" or "maxLength" => GetDecimal(after) < GetDecimal(before),
                "exclusiveMinimum" or "exclusiveMaximum" =>
                    !GetBoolean(before) && GetBoolean(after),
                "enum" => !IsEnumSuperset(before, after),
                "format" or "multipleOf" or "pattern" => true,
                _ => true,
            };
        }

        private static bool IsIneffectiveExclusiveFlag(string name, object value)
        {
            return name is "exclusiveMinimum" or "exclusiveMaximum" && !GetBoolean(value);
        }

        private static bool IsEnumSuperset(object before, object after)
        {
            var beforeValues = GetElement(before)
                .EnumerateArray()
                .Select(item => item.GetRawText())
                .ToHashSet(StringComparer.Ordinal);
            var afterValues = GetElement(after)
                .EnumerateArray()
                .Select(item => item.GetRawText())
                .ToHashSet(StringComparer.Ordinal);
            return beforeValues.IsSubsetOf(afterValues);
        }

        private static decimal GetDecimal(object value)
        {
            return GetElement(value).GetDecimal();
        }

        private static bool GetBoolean(object value)
        {
            return GetElement(value).GetBoolean();
        }

        private static bool JsonEquals(object left, object right)
        {
            return string.Equals(
                GetElement(left).GetRawText(),
                GetElement(right).GetRawText(),
                StringComparison.Ordinal
            );
        }

        private static JsonElement GetElement(object value)
        {
            return value is JsonElement element
                ? element
                : JsonSerializer.SerializeToElement(value);
        }
    }
}
