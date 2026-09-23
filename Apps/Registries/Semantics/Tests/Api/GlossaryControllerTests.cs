namespace Adr.Semantics.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using Adr.Semantics.Controllers;
    using Adr.Semantics.Models;
    using Adr.Semantics.Providers;
    using Adr.Semantics.Services;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging.Abstractions;

    public class GlossaryControllerTests : IDisposable
    {
        private static readonly Guid AccessControlId =
            Guid.Parse("a3ac060a-be57-4e70-af4c-bb7bd91bc7aa");

        private readonly GlossaryTestDatabase _database = new();

        [Fact]
        public void GetAllPreservesPayloadAndAddsCurrentGlossaryVersion()
        {
            var controller = CreateController();

            var response = controller.GetAll();

            Assert.Equal("connected-services", response.Glossary.Id);
            Assert.Equal("1.0.0", response.Glossary.Version);
            Assert.True(response.Glossary.IsCurrent);
            Assert.Equal(116, response.Payload.Count());
        }

        [Fact]
        public void GetVersionsReturnsTheComputedReleases()
        {
            var controller = CreateController();

            var releases = controller.GetVersions().Payload.ToList();
            var release = releases[0];

            Assert.Equal(2, releases.Count);
            Assert.Equal("connected-services", release.Id);
            Assert.Equal("1.0.0", release.Version);
            Assert.Equal(new DateOnly(2026, 9, 9), release.PublishedAt);
            Assert.True(release.IsCurrent);
        }

        [Fact]
        public void GetVersionReturnsAnImmutableGlossarySnapshot()
        {
            var controller = CreateController();

            var result = controller.GetVersion("0.1.0");
            var response = Assert.IsType<
                GlossaryResponseModel<IEnumerable<GlossaryModel>>
            >(Assert.IsType<OkObjectResult>(result.Result).Value);

            Assert.Equal("0.1.0", response.Glossary.Version);
            Assert.Equal(115, response.Payload.Count());
        }

        [Fact]
        public void GetVersionReturnsNotFoundForAnUnknownRelease()
        {
            var controller = CreateController();

            var result = controller.GetVersion("9.9.9");

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void GetByTermUsesTheHumanReadableSlug()
        {
            var controller = CreateController();

            var result = controller.GetByTerm("access-control");
            var response = Assert.IsType<GlossaryResponseModel<GlossaryModel>>(
                Assert.IsType<OkObjectResult>(result.Result).Value
            );

            Assert.Equal(AccessControlId.ToString(), response.Payload.StaticId);
            Assert.Equal("access-control", response.Payload.Name);
            Assert.Equal(1, response.Payload.Version);
            Assert.Equal("1.0.0", response.Glossary.Version);
        }

        [Fact]
        public void GetByTermDoesNotTreatTheLegacyUuidAsARouteIdentifier()
        {
            var controller = CreateController();

            var result = controller.GetByTerm(AccessControlId.ToString());

            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public void MarkdownIncludesGlossaryReleaseMetadata()
        {
            var controller = CreateController();

            var markdown = controller.GetMarkdownList().Content;

            Assert.Contains("glossary_id: connected-services", markdown);
            Assert.Contains("glossary_version: 1.0.0", markdown);
            Assert.Contains("glossary_published_at: 2026-09-09", markdown);
        }

        [Fact]
        public void SchemaContainsAReusableComponentForEveryPublishedTerm()
        {
            var controller = CreateController();

            using var document = JsonDocument.Parse(
                JsonSerializer.Serialize(controller.GetSchema().Value)
            );
            var root = document.RootElement;
            var schemas = root.GetProperty("components").GetProperty("schemas");
            var accessControl = schemas.GetProperty("access-control");
            var dataQuality = schemas.GetProperty("data-quality");
            var endpoint = schemas.GetProperty("endpoint");
            var validity = schemas.GetProperty("validity");

            Assert.Equal("3.0.3", root.GetProperty("openapi").GetString());
            Assert.Equal(
                "Connected Services Semantics Glossary",
                root.GetProperty("info").GetProperty("title").GetString()
            );
            Assert.Equal("1.0.0", root.GetProperty("info").GetProperty("version").GetString());
            Assert.Equal(116, schemas.EnumerateObject().Count());
            Assert.Equal("string", accessControl.GetProperty("type").GetString());
            Assert.Equal("Access Control", accessControl.GetProperty("title").GetString());
            Assert.False(accessControl.TryGetProperty("example", out _));
            AssertPercentageSchema(schemas, "accuracy", 99.8m);
            AssertPercentageSchema(schemas, "completeness", 98.5m);
            AssertPercentageSchema(schemas, "equity", 65.0m);
            AssertPercentageSchema(schemas, "uniqueness", 100.0m);
            Assert.Equal("string", dataQuality.GetProperty("type").GetString());
            Assert.Equal("string", validity.GetProperty("type").GetString());
            Assert.Equal("uri", endpoint.GetProperty("format").GetString());
            Assert.True(schemas.TryGetProperty("spouse", out _));
            Assert.True(schemas.TryGetProperty("msp-spouse", out _));
            Assert.False(
                schemas.TryGetProperty("spouse-income-and-disability-assistance", out _)
            );
            Assert.False(schemas.TryGetProperty("spouse-medical-services-plan", out _));
            Assert.Equal(
                "urn:bcgov:glossary:connected-services:access-control",
                accessControl.GetProperty("x-bc-semantic-urn").GetString()
            );
            Assert.False(accessControl.TryGetProperty("x-bc-semantic-glossary-version", out _));
            Assert.Equal(1, accessControl.GetProperty("x-bc-semantic-term-version").GetInt32());
            Assert.Equal(2, schemas.GetProperty("accuracy").GetProperty("x-bc-semantic-term-version").GetInt32());
            Assert.Equal(
                AccessControlId.ToString(),
                accessControl.GetProperty("x-bc-semantic-ref").GetString()
            );
            Assert.False(accessControl.TryGetProperty("x-bc-field", out _));
        }

        [Fact]
        public void VersionSchemaReturnsNotFoundForAnUnknownRelease()
        {
            var controller = CreateController();

            var result = controller.GetVersionSchema("9.9.9");

            Assert.IsType<NotFoundResult>(result);
        }

        private static void AssertPercentageSchema(
            JsonElement schemas,
            string name,
            decimal expectedExample
        )
        {
            var schema = schemas.GetProperty(name);

            Assert.Equal("number", schema.GetProperty("type").GetString());
            Assert.Equal(0, schema.GetProperty("minimum").GetDecimal());
            Assert.Equal(100, schema.GetProperty("maximum").GetDecimal());
            Assert.False(schema.TryGetProperty("format", out _));
            Assert.Equal(expectedExample, schema.GetProperty("example").GetDecimal());
        }

        [Fact]
        public void CurrentAndVersionedSchemaRoutesShareTheCachedReleaseDocument()
        {
            var controller = CreateController();

            var currentDocument = controller.GetSchema().Value;
            var currentDocumentAgain = controller.GetSchema().Value;
            var versionedDocument = Assert.IsType<JsonResult>(
                controller.GetVersionSchema("1.0.0")
            ).Value;

            Assert.Same(currentDocument, currentDocumentAgain);
            Assert.Same(currentDocument, versionedDocument);
        }

        [Fact]
        public void GetTermVersionsReturnsPersistedTermHistory()
        {
            var controller = CreateController();

            var result = controller.GetTermVersions("accuracy");
            var response = Assert.IsType<BaseResponseModel<IEnumerable<GlossaryModel>>>(
                Assert.IsType<OkObjectResult>(result.Result).Value
            );

            Assert.Equal([2, 1], response.Payload.Select(item => item.Version));
            Assert.IsType<NotFoundResult>(
                controller.GetTermVersion("accuracy", 3).Result
            );
        }

        [Fact]
        public void TermVersionSchemaReturnsTheCachedCanonicalRevision()
        {
            var controller = CreateController();

            var result = Assert.IsType<JsonResult>(
                controller.GetTermVersionSchema("accuracy", 1)
            );
            var repeatedResult = Assert.IsType<JsonResult>(
                controller.GetTermVersionSchema("accuracy", 1)
            );

            Assert.Same(result.Value, repeatedResult.Value);
            using var document = JsonDocument.Parse(JsonSerializer.Serialize(result.Value));
            var schema = document.RootElement;
            Assert.Equal("string", schema.GetProperty("type").GetString());
            Assert.Equal("Accuracy", schema.GetProperty("title").GetString());
            Assert.Equal(
                "urn:bcgov:glossary:connected-services:accuracy",
                schema.GetProperty("x-bc-semantic-urn").GetString()
            );
            Assert.Equal(1, schema.GetProperty("x-bc-semantic-term-version").GetInt32());
            Assert.False(schema.TryGetProperty("x-bc-semantic-glossary-version", out _));
            Assert.Equal(
                "public, max-age=31536000, immutable",
                controller.Response.Headers.CacheControl
            );
            Assert.IsType<NotFoundResult>(
                controller.GetTermVersionSchema("accuracy", 3)
            );
        }

        public void Dispose()
        {
            _database.Dispose();
            GC.SuppressFinalize(this);
        }

        private GlossaryController CreateController()
        {
            var service = new GlossaryService(
                NullLogger<GlossaryService>.Instance,
                _database.Provider
            );
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            var editingService = new GlossaryEditingService(
                _database.Factory,
                _database.Publisher,
                _database.Processor
            );
            return new GlossaryController(
                NullLogger<GlossaryController>.Instance,
                service,
                editingService,
                memoryCache
            )
            {
                ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            };
        }
    }
}
