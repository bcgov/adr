namespace Adr.Semantics.Mappers
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using Adr.Semantics.Models;

    /// <summary>
    /// Maps an OpenAPI spec JSON document into a <see cref="DictionaryEntryModel"/>
    /// by walking <c>components.schemas[*].allOf[1].properties</c> and extracting the
    /// BC dictionary extension fields (<c>x-bc-*</c>). Mirrors the front-end logic in
    /// PublicBodies/Source/front-end/src/components/DictionaryTable/util.ts.
    /// </summary>
    public sealed class DictionaryMapper
    {
        private const string FieldExtension = "x-bc-field";

        /// <summary>
        /// Parses an OpenAPI spec JSON string into a <see cref="DictionaryEntryModel"/>.
        /// </summary>
        /// <param name="name">Logical name of the source (e.g. the configuration key). Used as the entry name and the basis for its id.</param>
        /// <param name="sourceUrl">The URL the spec was fetched from. Recorded on the entry as its source.</param>
        /// <param name="json">The raw OpenAPI spec JSON.</param>
        /// <returns>A populated <see cref="DictionaryEntryModel"/>.</returns>
        public DictionaryEntryModel Map(string name, string sourceUrl, string json)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (string.IsNullOrWhiteSpace(name))
            {
                name = sourceUrl;
                if (root.TryGetProperty("info", out var info)
                    && info.TryGetProperty("title", out var title)
                    && title.ValueKind == JsonValueKind.String)
                {
                    name = title.GetString() ?? sourceUrl;
                }
            }

            var fields = new List<DictionaryEntryFieldModel>();

            if (root.TryGetProperty("components", out var components)
                && components.TryGetProperty("schemas", out var schemas)
                && schemas.ValueKind == JsonValueKind.Object)
            {
                foreach (var schema in schemas.EnumerateObject())
                {
                    fields.AddRange(ExtractSchemaFields(schema.Value));
                }
            }

            return new DictionaryEntryModel
            {
                Id = name.Replace(" ", "-").ToLowerInvariant(),
                Name = name,
                Source = sourceUrl,
                Fields = fields,
            };
        }

        private static IEnumerable<DictionaryEntryFieldModel> ExtractSchemaFields(JsonElement schema)
        {
            if (schema.ValueKind != JsonValueKind.Object
                || !schema.TryGetProperty("allOf", out var allOf)
                || allOf.ValueKind != JsonValueKind.Array)
            {
                yield break;
            }

            var allOfItems = allOf.EnumerateArray().ToList();
            if (allOfItems.Count != 2
                || !allOfItems[1].TryGetProperty("properties", out var properties)
                || properties.ValueKind != JsonValueKind.Object)
            {
                yield break;
            }

            foreach (var prop in properties.EnumerateObject())
            {
                var field = ExtractField(prop.Value);
                if (field is not null)
                {
                    yield return field;
                }
            }
        }

        private static DictionaryEntryFieldModel? ExtractField(JsonElement field)
        {
            if (field.ValueKind != JsonValueKind.Object
                || !field.TryGetProperty(FieldExtension, out _))
            {
                return null;
            }

            return new DictionaryEntryFieldModel
            {
                FieldName = ReadString(field, "x-bc-field"),
                FieldDescription = ReadString(field, "x-bc-desc"),
                SchemaNameTableName = ReadString(field, "x-bc-schema-table"),
                DataSource = ReadString(field, "x-bc-source"),
                DataType = ReadString(field, "x-bc-type"),
                KeyRelationships = ReadString(field, "x-bc-key"),
                SystemOfRecord = ReadString(field, "x-bc-sor"),
                DesignatedAsRequired = ReadString(field, "x-bc-req"),
                SemanticTermRef = ReadString(field, "x-bc-semantic-ref"),
            };
        }

        private static string ReadString(JsonElement element, string property)
        {
            if (element.TryGetProperty(property, out var value)
                && value.ValueKind == JsonValueKind.String)
            {
                return value.GetString() ?? string.Empty;
            }

            return string.Empty;
        }
    }
}
