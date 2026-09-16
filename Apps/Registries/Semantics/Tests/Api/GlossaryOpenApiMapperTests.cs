namespace Adr.Semantics.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using Adr.Semantics.Mappers;
    using Adr.Semantics.Models;

    public class GlossaryOpenApiMapperTests
    {
        [Theory]
        [InlineData("number", "12.5", JsonValueKind.Number)]
        [InlineData("integer", "42", JsonValueKind.Number)]
        [InlineData("boolean", "true", JsonValueKind.True)]
        public void MapsTypedExamples(
            string schemaType,
            string example,
            JsonValueKind expectedExampleKind
        )
        {
            using var document = MapTerm(schemaType, "", example);
            var schema = GetTermSchema(document);

            Assert.Equal(schemaType, schema.GetProperty("type").GetString());
            Assert.Equal("Example Term", schema.GetProperty("title").GetString());
            Assert.Equal(expectedExampleKind, schema.GetProperty("example").ValueKind);
            Assert.Equal(7, schema.GetProperty("x-bc-semantic-term-version").GetInt32());
            Assert.Equal(
                "Example Glossary",
                document.RootElement.GetProperty("info").GetProperty("title").GetString()
            );
            Assert.Equal(
                "1.0.0",
                document.RootElement.GetProperty("info").GetProperty("version").GetString()
            );
            Assert.False(schema.TryGetProperty("x-bc-semantic-glossary-version", out _));
            Assert.Equal(
                "urn:bcgov:glossary:example-glossary:example-term",
                schema.GetProperty("x-bc-semantic-urn").GetString()
            );
        }

        [Fact]
        public void SemanticUrnRemainsStableAcrossTermVersions()
        {
            using var firstDocument = MapTerm("string", "", "", 1);
            using var secondDocument = MapTerm("string", "", "", 2);
            var firstSchema = GetTermSchema(firstDocument);
            var secondSchema = GetTermSchema(secondDocument);

            Assert.Equal(
                firstSchema.GetProperty("x-bc-semantic-urn").GetString(),
                secondSchema.GetProperty("x-bc-semantic-urn").GetString()
            );
            Assert.Equal(1, firstSchema.GetProperty("x-bc-semantic-term-version").GetInt32());
            Assert.Equal(2, secondSchema.GetProperty("x-bc-semantic-term-version").GetInt32());
        }

        [Fact]
        public void DefaultsToStringAndOmitsBlankConstraints()
        {
            using var document = MapTerm("", "", "example value");
            var schema = GetTermSchema(document);

            Assert.Equal("string", schema.GetProperty("type").GetString());
            Assert.Equal("example value", schema.GetProperty("example").GetString());
            Assert.False(schema.TryGetProperty("format", out _));
        }

        [Fact]
        public void FallsBackToStringForAnUnsupportedSchemaType()
        {
            using var document = MapTerm(
                "object",
                """{"format":"custom-format"}""",
                "example value"
            );
            var schema = GetTermSchema(document);

            Assert.Equal("string", schema.GetProperty("type").GetString());
            Assert.Equal("custom-format", schema.GetProperty("format").GetString());
            Assert.Equal("example value", schema.GetProperty("example").GetString());
        }

        private static JsonDocument MapTerm(
            string schemaType,
            string schemaConstraints,
            string example,
            int version = 7
        )
        {
            var normalizedType = GlossarySchemaType.Normalize(schemaType);
            var term = new GlossaryModel
            {
                Version = version,
                Term = "Example Term",
                Name = "example-term",
                Definition = "An example term.",
                StaticId = "550e8400-e29b-41d4-a716-446655440000",
                SchemaType = schemaType,
                SchemaConstraints = GlossarySchemaConstraints
                    .Parse(schemaConstraints, normalizedType)
                    .Constraints,
                Example = example,
                PublishToDevHub = true,
                VerifiedDefinitionFlag = true,
            };
            var glossary = new GlossaryVersionModel
            {
                Id = "example-glossary",
                Name = "Example Glossary",
                Version = "1.0.0",
                PublishedAt = new DateOnly(2026, 9, 10),
                IsCurrent = true,
            };

            return JsonDocument.Parse(
                JsonSerializer.Serialize(GlossaryOpenApiMapper.Map([term], glossary))
            );
        }

        private static JsonElement GetTermSchema(JsonDocument document)
        {
            return document.RootElement
                .GetProperty("components")
                .GetProperty("schemas")
                .GetProperty("example-term");
        }
    }
}
