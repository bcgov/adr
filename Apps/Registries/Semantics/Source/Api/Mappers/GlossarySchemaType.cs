namespace Adr.Semantics.Mappers
{
    using System.Collections.Generic;

    internal static class GlossarySchemaType
    {
        private const string DefaultType = "string";

        private static readonly HashSet<string> SupportedTypes = new(
            [DefaultType, "number", "integer", "boolean"],
            System.StringComparer.OrdinalIgnoreCase
        );

        internal static bool IsSupported(string? schemaType)
        {
            return string.IsNullOrWhiteSpace(schemaType)
                || SupportedTypes.Contains(schemaType.Trim());
        }

        internal static string Normalize(string? schemaType)
        {
            return IsSupported(schemaType)
                ? string.IsNullOrWhiteSpace(schemaType)
                    ? DefaultType
                    : schemaType.Trim().ToLowerInvariant()
                : DefaultType;
        }
    }
}
