namespace Adr.Semantics.Mappers
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using Adr.Semantics.Models;

    /// <summary>
    /// Maps a glossary release to an OpenAPI document containing reusable term schemas.
    /// </summary>
    public static class GlossaryOpenApiMapper
    {
        /// <summary>
        /// Maps published glossary terms to OpenAPI schema components keyed by term slug.
        /// </summary>
        /// <param name="glossaryInfo">The glossary terms.</param>
        /// <param name="glossary">The glossary release metadata.</param>
        /// <returns>An OpenAPI 3.0 document.</returns>
        public static Dictionary<string, object> Map(
            IEnumerable<GlossaryModel> glossaryInfo,
            GlossaryVersionModel glossary
        )
        {
            var schemas = glossaryInfo
                .Where(entry => entry.PublishToDevHub && entry.VerifiedDefinitionFlag)
                .OrderBy(entry => entry.Name, System.StringComparer.Ordinal)
                .ToDictionary(
                    entry => entry.Name,
                    entry => (object)MapSchema(entry, glossary.Id),
                    System.StringComparer.Ordinal
                );

            var document = new Dictionary<string, object>
            {
                ["openapi"] = "3.0.3",
                ["info"] = new Dictionary<string, object>
                {
                    ["title"] = glossary.Name,
                    ["version"] = glossary.Version,
                },
                ["paths"] = new Dictionary<string, object>(),
                ["components"] = new Dictionary<string, object> { ["schemas"] = schemas },
            };

            return document;
        }

        /// <summary>
        /// Maps one persisted term revision to a reusable OpenAPI Schema Object.
        /// </summary>
        /// <param name="entry">The persisted term revision.</param>
        /// <param name="glossaryId">The glossary identifier.</param>
        /// <returns>An OpenAPI Schema Object.</returns>
        public static Dictionary<string, object> MapSchema(GlossaryModel entry, string glossaryId)
        {
            var schemaType = GlossarySchemaType.Normalize(entry.SchemaType);
            var schema = new Dictionary<string, object>
            {
                ["type"] = schemaType,
                ["title"] = entry.Term,
                ["description"] = entry.Definition,
            };

            foreach (var constraint in entry.SchemaConstraints)
            {
                schema[constraint.Key] = constraint.Value;
            }

            if (!string.IsNullOrWhiteSpace(entry.Example))
            {
                schema["example"] = MapExample(entry.Example, schemaType);
            }

            schema["x-bc-semantic-urn"] =
                $"urn:bcgov:glossary:{glossaryId}:{entry.Name}";
            schema["x-bc-semantic-term-version"] = entry.Version;
            schema["x-bc-semantic-ref"] = entry.StaticId;

            return schema;
        }

        private static object MapExample(string example, string schemaType)
        {
            return schemaType switch
            {
                "number" => decimal.Parse(example, NumberStyles.Float, CultureInfo.InvariantCulture),
                "integer" => long.Parse(example, NumberStyles.Integer, CultureInfo.InvariantCulture),
                "boolean" => bool.Parse(example),
                _ => example,
            };
        }
    }
}
