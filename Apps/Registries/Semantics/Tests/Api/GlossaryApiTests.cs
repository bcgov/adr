namespace Adr.Semantics.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Json;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Adr.Semantics.Data;
    using Adr.Semantics.Models;
    using Adr.Semantics.Providers;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;

    public class GlossaryApiTests : IClassFixture<GlossaryApiFactory>
    {
        private readonly HttpClient _client;

        public GlossaryApiTests(GlossaryApiFactory application)
        {
            _client = application.CreateClient();
        }

        [Fact]
        public async Task CurrentGlossaryRouteReturnsVersionedResponse()
        {
            using var response = await _client.GetAsync("/v1/Glossary");
            var glossary = await response.Content.ReadFromJsonAsync<
                GlossaryResponseModel<IList<GlossaryModel>>
            >();

            response.EnsureSuccessStatusCode();
            Assert.NotNull(glossary);
            Assert.Equal("1.0.0", glossary.Glossary.Version);
            Assert.Equal(116, glossary.Payload.Count);
        }

        [Fact]
        public async Task VersionAndUuidLookupRoutesAreReachable()
        {
            using var versionResponse = await _client.GetAsync("/v1/Glossary/versions/1.0.0");
            using var initialVersionResponse = await _client.GetAsync(
                "/v1/Glossary/versions/0.1.0"
            );
            using var uuidResponse = await _client.GetAsync(
                "/v1/Glossary/id/a3ac060a-be57-4e70-af4c-bb7bd91bc7aa"
            );
            using var unknownUuidResponse = await _client.GetAsync(
                "/v1/Glossary/id/11111111-1111-1111-1111-111111111111"
            );
            using var missingVersionResponse = await _client.GetAsync(
                "/v1/Glossary/versions/9.9.9"
            );

            Assert.Equal(HttpStatusCode.OK, versionResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, initialVersionResponse.StatusCode);
            uuidResponse.EnsureSuccessStatusCode();
            var entry = await uuidResponse.Content.ReadFromJsonAsync<
                GlossaryResponseModel<GlossaryModel>
            >();
            Assert.Equal("access-control", entry!.Payload.Name);
            Assert.Equal(
                "a3ac060a-be57-4e70-af4c-bb7bd91bc7aa",
                entry.Payload.StaticId
            );
            Assert.Equal(HttpStatusCode.NotFound, unknownUuidResponse.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, missingVersionResponse.StatusCode);
        }

        [Fact]
        public async Task ReleaseScopedVersionsIncludePublishedAndInvalidEntriesNewestFirst()
        {
            using var response = await _client.GetAsync(
                "/v1/Glossary/versions/1.0.0/terms/msp-spouse/versions"
            );

            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadFromJsonAsync<
                BaseResponseModel<IList<GlossaryTermHistoryModel>>
            >();

            Assert.NotNull(result);
            Assert.Equal(2, result.Payload.Count);
            var published = result.Payload[0];
            Assert.Equal("Published", published.Status);
            Assert.Equal(1, published.Version);
            Assert.Equal("msp-spouse", published.Term!.Name);
            Assert.Contains("1.0.0", published.GlossaryVersions);

            var invalid = result.Payload[1];
            Assert.Equal("Invalid", invalid.Status);
            Assert.Null(invalid.Version);
            Assert.Null(invalid.Term);
            Assert.Equal("spouse", invalid.Name);
            Assert.NotEmpty(invalid.InvalidReasons);
            Assert.False(string.IsNullOrWhiteSpace(invalid.SourcePayload));
            Assert.True(published.RecordedUtc >= invalid.RecordedUtc);
        }

        [Fact]
        public async Task OlderGlossaryReleaseCanResolveVersionsForADeletedTerm()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            using var initialResponse = await client.GetAsync("/v1/Glossary/applicant");
            var initial = await initialResponse.Content.ReadFromJsonAsync<
                GlossaryResponseModel<GlossaryModel>
            >();
            using var createResponse = await client.PostAsync("/v1/Glossary/drafts", null);
            var created = await createResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var draftId = created!.Payload.Draft.Id;

            using var deleteResponse = await client.DeleteAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/applicant"
            );
            using var publishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = draftId }
            );
            using var currentIdResponse = await client.GetAsync(
                $"/v1/Glossary/id/{initial!.Payload.StaticId}"
            );
            using var scopedVersionsResponse = await client.GetAsync(
                "/v1/Glossary/versions/1.0.0/terms/applicant/versions"
            );
            using var unscopedVersionsResponse = await client.GetAsync(
                "/v1/Glossary/terms/applicant/versions"
            );

            initialResponse.EnsureSuccessStatusCode();
            createResponse.EnsureSuccessStatusCode();
            deleteResponse.EnsureSuccessStatusCode();
            publishResponse.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NotFound, currentIdResponse.StatusCode);
            scopedVersionsResponse.EnsureSuccessStatusCode();
            unscopedVersionsResponse.EnsureSuccessStatusCode();

            var scoped = await scopedVersionsResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<IList<GlossaryTermHistoryModel>>
            >();
            var selected = Assert.Single(scoped!.Payload, item => item.Version == 1);
            Assert.Equal("applicant", selected.Term!.Name);
            Assert.Equal(1, selected.Version);

            var unscoped = await unscopedVersionsResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<IList<GlossaryModel>>
            >();
            Assert.Equal("applicant", Assert.Single(unscoped!.Payload).Name);
        }

        [Fact]
        public async Task ReleaseScopedVersionsIncludeRevisionsAddedByLaterGlossaries()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            using var initialResponse = await client.GetAsync("/v1/Glossary/access-control");
            var initial = await initialResponse.Content.ReadFromJsonAsync<
                GlossaryResponseModel<GlossaryModel>
            >();
            using var createResponse = await client.PostAsync("/v1/Glossary/drafts", null);
            var created = await createResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var edit = ToEdit(initial!.Payload);
            edit.Definition += " Clarified in a later glossary release.";

            using var editResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{created!.Payload.Draft.Id}/terms/access-control",
                edit
            );
            using var publishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = created.Payload.Draft.Id }
            );
            using var versionsResponse = await client.GetAsync(
                "/v1/Glossary/versions/1.0.0/terms/access-control/versions"
            );

            initialResponse.EnsureSuccessStatusCode();
            createResponse.EnsureSuccessStatusCode();
            editResponse.EnsureSuccessStatusCode();
            publishResponse.EnsureSuccessStatusCode();
            versionsResponse.EnsureSuccessStatusCode();

            var history = await versionsResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<IList<GlossaryTermHistoryModel>>
            >();
            var published = history!.Payload.Where(item => item.Status == "Published").ToList();
            Assert.Equal([2, 1], published.Select(item => item.Version));
            Assert.Contains("1.0.1", published[0].GlossaryVersions);
            Assert.Contains("1.0.0", published[1].GlossaryVersions);
        }

        [Fact]
        public async Task CorrectedStaticIdRetainsTermLineageAndImmutableRevisionLookups()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            var initial = await client.GetFromJsonAsync<GlossaryResponseModel<GlossaryModel>>(
                "/v1/Glossary/access-control"
            );
            var originalStaticId = Guid.Parse(initial!.Payload.StaticId);
            var correctedStaticId = Guid.Parse("22222222-2222-2222-2222-222222222222");
            using var createResponse = await client.PostAsync("/v1/Glossary/drafts", null);
            var created = await createResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var edit = ToEdit(initial.Payload);
            edit.StaticId = correctedStaticId.ToString();

            using var editResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{created!.Payload.Draft.Id}/terms/access-control",
                edit
            );
            using var previewResponse = await client.GetAsync(
                $"/v1/Glossary/drafts/{created.Payload.Draft.Id}"
            );
            var preview = await previewResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            using var publishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = created.Payload.Draft.Id }
            );
            using var firstRevisionResponse = await client.GetAsync(
                "/v1/Glossary/terms/access-control/versions/1"
            );
            var firstRevision = await firstRevisionResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryModel>
            >();
            using var secondRevisionResponse = await client.GetAsync(
                "/v1/Glossary/terms/access-control/versions/2"
            );
            var secondRevision = await secondRevisionResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryModel>
            >();
            using var firstSchemaResponse = await client.GetAsync(
                "/v1/Glossary/terms/access-control/versions/1/schema"
            );
            using var firstSchema = JsonDocument.Parse(
                await firstSchemaResponse.Content.ReadAsStringAsync()
            );
            using var secondSchemaResponse = await client.GetAsync(
                "/v1/Glossary/terms/access-control/versions/2/schema"
            );
            using var secondSchema = JsonDocument.Parse(
                await secondSchemaResponse.Content.ReadAsStringAsync()
            );
            using var oldIdResponse = await client.GetAsync(
                $"/v1/Glossary/id/{originalStaticId}"
            );
            using var correctedIdResponse = await client.GetAsync(
                $"/v1/Glossary/id/{correctedStaticId}"
            );
            using var historyResponse = await client.GetAsync(
                "/v1/Glossary/versions/1.0.0/terms/access-control/versions"
            );
            var history = await historyResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<IList<GlossaryTermHistoryModel>>
            >();

            editResponse.EnsureSuccessStatusCode();
            previewResponse.EnsureSuccessStatusCode();
            publishResponse.EnsureSuccessStatusCode();
            firstRevisionResponse.EnsureSuccessStatusCode();
            secondRevisionResponse.EnsureSuccessStatusCode();
            firstSchemaResponse.EnsureSuccessStatusCode();
            secondSchemaResponse.EnsureSuccessStatusCode();
            correctedIdResponse.EnsureSuccessStatusCode();
            historyResponse.EnsureSuccessStatusCode();
            Assert.Equal("2.0.0-alpha", preview!.Payload.Draft.Version);
            Assert.Equal(
                2,
                preview.Payload.Terms.Single(item => item.Name == "access-control").Version
            );
            Assert.Equal(originalStaticId.ToString(), firstRevision!.Payload.StaticId);
            Assert.Equal(correctedStaticId.ToString(), secondRevision!.Payload.StaticId);
            Assert.Equal(
                originalStaticId.ToString(),
                firstSchema.RootElement.GetProperty("x-bc-semantic-ref").GetString()
            );
            Assert.Equal(
                correctedStaticId.ToString(),
                secondSchema.RootElement.GetProperty("x-bc-semantic-ref").GetString()
            );
            Assert.Equal(HttpStatusCode.NotFound, oldIdResponse.StatusCode);
            Assert.Equal(
                [2, 1],
                history!.Payload
                    .Where(item => item.Status == "Published")
                    .Select(item => item.Version)
            );

            using var scope = application.Services.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<
                IDbContextFactory<GlossaryDbContext>
            >();
            using var context = contextFactory.CreateDbContext();
            var revisions = context
                .GlossaryTermRevisions.Where(item => item.Name == "access-control")
                .OrderBy(item => item.Revision)
                .ToList();
            Assert.Equal([1, 2], revisions.Select(item => item.Revision));
            Assert.Single(revisions.Select(item => item.TermId).Distinct());
            Assert.Equal(
                [originalStaticId, correctedStaticId],
                revisions.Select(item => item.StaticId)
            );
        }

        [Fact]
        public async Task ReusingHistoricalStaticIdDoesNotMergeTermLineages()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            var initial = await client.GetFromJsonAsync<GlossaryResponseModel<GlossaryModel>>(
                "/v1/Glossary/access-control"
            );
            var originalStaticId = initial!.Payload.StaticId;
            using var correctionDraftResponse = await client.PostAsync(
                "/v1/Glossary/drafts",
                null
            );
            var correctionDraft = await correctionDraftResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var correction = ToEdit(initial.Payload);
            correction.StaticId = "22222222-2222-2222-2222-222222222222";
            using var correctionResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{correctionDraft!.Payload.Draft.Id}/terms/access-control",
                correction
            );
            using var correctionPublishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel
                {
                    DraftId = correctionDraft.Payload.Draft.Id,
                }
            );
            using var newTermDraftResponse = await client.PostAsync(
                "/v1/Glossary/drafts",
                null
            );
            var newTermDraft = await newTermDraftResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            using var newTermResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{newTermDraft!.Payload.Draft.Id}/terms/review-distinct-term",
                new GlossaryTermEditModel
                {
                    StaticId = originalStaticId,
                    Term = "Review Distinct Term",
                    Definition = "A distinct term that reuses a legacy UUID from an older revision.",
                    VerifiedDefinitionFlag = true,
                    PublishToDevHub = true,
                }
            );
            using var newTermPreviewResponse = await client.GetAsync(
                $"/v1/Glossary/drafts/{newTermDraft.Payload.Draft.Id}"
            );
            var newTermPreview = await newTermPreviewResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            using var newTermPublishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = newTermDraft.Payload.Draft.Id }
            );
            using var accessControlHistoryResponse = await client.GetAsync(
                "/v1/Glossary/terms/access-control/versions"
            );
            var accessControlHistory = await accessControlHistoryResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<IList<GlossaryModel>>
            >();
            using var newTermHistoryResponse = await client.GetAsync(
                "/v1/Glossary/terms/review-distinct-term/versions"
            );
            var newTermHistory = await newTermHistoryResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<IList<GlossaryModel>>
            >();
            using var accessControlSchemaResponse = await client.GetAsync(
                "/v1/Glossary/terms/access-control/versions/1/schema"
            );
            using var accessControlSchema = JsonDocument.Parse(
                await accessControlSchemaResponse.Content.ReadAsStringAsync()
            );
            using var newTermSchemaResponse = await client.GetAsync(
                "/v1/Glossary/terms/review-distinct-term/versions/1/schema"
            );
            using var newTermSchema = JsonDocument.Parse(
                await newTermSchemaResponse.Content.ReadAsStringAsync()
            );

            correctionDraftResponse.EnsureSuccessStatusCode();
            correctionResponse.EnsureSuccessStatusCode();
            correctionPublishResponse.EnsureSuccessStatusCode();
            newTermDraftResponse.EnsureSuccessStatusCode();
            newTermResponse.EnsureSuccessStatusCode();
            newTermPreviewResponse.EnsureSuccessStatusCode();
            newTermPublishResponse.EnsureSuccessStatusCode();
            accessControlHistoryResponse.EnsureSuccessStatusCode();
            newTermHistoryResponse.EnsureSuccessStatusCode();
            accessControlSchemaResponse.EnsureSuccessStatusCode();
            newTermSchemaResponse.EnsureSuccessStatusCode();
            Assert.Equal(
                1,
                newTermPreview!.Payload.Terms.Single(item =>
                    item.Name == "review-distinct-term"
                ).Version
            );
            Assert.Equal([2, 1], accessControlHistory!.Payload.Select(item => item.Version));
            Assert.Equal(1, Assert.Single(newTermHistory!.Payload).Version);
            Assert.Equal(
                "urn:bcgov:glossary:connected-services:access-control",
                accessControlSchema.RootElement.GetProperty("x-bc-semantic-urn").GetString()
            );
            Assert.Equal(
                "urn:bcgov:glossary:connected-services:review-distinct-term",
                newTermSchema.RootElement.GetProperty("x-bc-semantic-urn").GetString()
            );
            Assert.NotEqual(
                accessControlSchemaResponse.Headers.ETag,
                newTermSchemaResponse.Headers.ETag
            );

            using var scope = application.Services.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<
                IDbContextFactory<GlossaryDbContext>
            >();
            using var context = contextFactory.CreateDbContext();
            var accessControlIdentity = context.GlossaryTerms.Single(item =>
                item.Name == "access-control"
            );
            var newTermIdentity = context.GlossaryTerms.Single(item =>
                item.Name == "review-distinct-term"
            );
            Assert.NotEqual(accessControlIdentity.Id, newTermIdentity.Id);
        }

        [Fact]
        public async Task OpenApiDeclaresGlossarySchemaRoutesAsAnonymous()
        {
            using var response = await _client.GetAsync("/swagger/v1/swagger.json");

            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var paths = document.RootElement.GetProperty("paths");

            AssertAnonymous(paths.GetProperty("/v1/Glossary/schema").GetProperty("get"));
            AssertAnonymous(
                paths
                    .GetProperty("/v1/Glossary/versions/{glossaryVersion}/schema")
                    .GetProperty("get")
            );
            AssertAnonymous(
                paths
                    .GetProperty(
                        "/v1/Glossary/terms/{term}/versions/{termVersion}/schema"
                    )
                    .GetProperty("get")
            );
        }

        [Fact]
        public async Task OpenApiDeclaresTheGlossaryEditingContract()
        {
            using var response = await _client.GetAsync("/swagger/v1/swagger.json");

            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = document.RootElement;
            var list = root
                .GetProperty("paths")
                .GetProperty("/v1/Glossary/drafts")
                .GetProperty("get");
            var put = root
                .GetProperty("paths")
                .GetProperty("/v1/Glossary/drafts/{draftId}/terms/{term}")
                .GetProperty("put");
            var rebase = root
                .GetProperty("paths")
                .GetProperty("/v1/Glossary/drafts/{draftId}/rebase")
                .GetProperty("post");
            var publish = root
                .GetProperty("paths")
                .GetProperty("/v1/Glossary/versions")
                .GetProperty("post");
            var editProperties = root
                .GetProperty("components")
                .GetProperty("schemas")
                .GetProperty("Adr.Semantics.Models.GlossaryTermEditModel")
                .GetProperty("properties");

            var statusParameter = Assert.Single(
                list.GetProperty("parameters").EnumerateArray(),
                parameter => parameter.GetProperty("name").GetString() == "status"
            );
            Assert.True(
                !statusParameter.TryGetProperty("required", out var required)
                    || !required.GetBoolean()
            );
            Assert.True(put.GetProperty("requestBody").GetProperty("required").GetBoolean());
            Assert.True(rebase.GetProperty("responses").TryGetProperty("200", out _));
            Assert.True(rebase.GetProperty("responses").TryGetProperty("409", out _));
            Assert.True(editProperties.TryGetProperty("id", out _));
            Assert.True(editProperties.TryGetProperty("breakingChange", out _));
            Assert.False(editProperties.TryGetProperty("staticId", out _));
            Assert.True(publish.GetProperty("requestBody").GetProperty("required").GetBoolean());
            Assert.Equal(
                "#/components/schemas/Adr.Semantics.Models.GlossaryVersionPublishModel",
                publish
                    .GetProperty("requestBody")
                    .GetProperty("content")
                    .GetProperty("application/json")
                    .GetProperty("schema")
                    .GetProperty("$ref")
                    .GetString()
            );
            Assert.False(
                root.GetProperty("paths")
                    .TryGetProperty("/v1/Glossary/drafts/{draftId}/publish", out _)
            );
        }

        [Fact]
        public async Task OpenApiIncludesTheRequiredDocumentationMetadata()
        {
            using var response = await _client.GetAsync("/swagger/v1/swagger.json");

            response.EnsureSuccessStatusCode();
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = document.RootElement;
            var definedTags = root
                .GetProperty("tags")
                .EnumerateArray()
                .Select(tag => tag.GetProperty("name").GetString())
                .ToHashSet(StringComparer.Ordinal);
            var operationNames = new HashSet<string>(StringComparer.Ordinal)
            {
                "delete",
                "get",
                "patch",
                "post",
                "put",
            };

            Assert.Contains("Dictionary", definedTags);
            Assert.Contains("Glossary", definedTags);
            Assert.Contains("GlossaryDraft", definedTags);
            foreach (var path in root.GetProperty("paths").EnumerateObject())
            {
                Assert.False(string.IsNullOrWhiteSpace(path.Value.GetProperty("summary").GetString()));
                foreach (
                    var operation in path.Value.EnumerateObject()
                        .Where(property => operationNames.Contains(property.Name))
                )
                {
                    var operationValue = operation.Value;
                    var summary = operationValue.GetProperty("summary").GetString();
                    var description = operationValue.GetProperty("description").GetString();
                    Assert.False(string.IsNullOrWhiteSpace(summary));
                    Assert.True(description?.Length > summary?.Length);
                    foreach (var tag in operationValue.GetProperty("tags").EnumerateArray())
                    {
                        Assert.Contains(tag.GetString(), definedTags);
                    }

                    if (operationValue.TryGetProperty("parameters", out var parameters))
                    {
                        foreach (var parameter in parameters.EnumerateArray())
                        {
                            Assert.True(parameter.GetProperty("description").GetString()?.Length >= 25);
                        }
                    }

                    foreach (
                        var operationResponse in operationValue
                            .GetProperty("responses")
                            .EnumerateObject()
                    )
                    {
                        Assert.True(
                            operationResponse.Value.GetProperty("description").GetString()?.Length
                                >= 20
                        );
                    }
                }
            }

            foreach (
                var schema in root
                    .GetProperty("components")
                    .GetProperty("schemas")
                    .EnumerateObject()
            )
            {
                Assert.True(schema.Value.GetProperty("description").GetString()?.Length >= 30);
                Assert.True(schema.Value.TryGetProperty("example", out _));
            }
        }

        [Fact]
        public async Task DraftCollectionListsAndFiltersDraftMetadata()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            using var firstCreateResponse = await client.PostAsync(
                "/v1/Glossary/drafts",
                null
            );
            var first = await firstCreateResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var edit = new GlossaryTermEditModel
            {
                Term = "New Semantic Term",
                Definition = "A term added through the glossary editing API.",
                VerifiedDefinitionFlag = true,
                PublishToDevHub = true,
            };

            using var editResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{first!.Payload.Draft.Id}/terms/new-semantic-term",
                edit
            );
            using var publishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = first.Payload.Draft.Id }
            );
            using var secondCreateResponse = await client.PostAsync(
                "/v1/Glossary/drafts",
                null
            );
            var second = await secondCreateResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();

            editResponse.EnsureSuccessStatusCode();
            publishResponse.EnsureSuccessStatusCode();
            secondCreateResponse.EnsureSuccessStatusCode();

            using var allResponse = await client.GetAsync("/v1/Glossary/drafts");
            var all = await allResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<IList<GlossaryDraftModel>>
            >();
            using var draftResponse = await client.GetAsync(
                "/v1/Glossary/drafts?status=Draft"
            );
            var drafts = await draftResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<IList<GlossaryDraftModel>>
            >();
            using var publishedResponse = await client.GetAsync(
                "/v1/Glossary/drafts?status=published"
            );
            var published = await publishedResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<IList<GlossaryDraftModel>>
            >();

            allResponse.EnsureSuccessStatusCode();
            draftResponse.EnsureSuccessStatusCode();
            publishedResponse.EnsureSuccessStatusCode();
            Assert.Equal(
                [second!.Payload.Draft.Id, first.Payload.Draft.Id],
                all!.Payload.Select(item => item.Id)
            );
            Assert.Equal(second.Payload.Draft.Id, Assert.Single(drafts!.Payload).Id);
            Assert.Equal("Draft", drafts.Payload[0].Status);
            Assert.Equal(first.Payload.Draft.Id, Assert.Single(published!.Payload).Id);
            Assert.Equal("Published", published.Payload[0].Status);
            Assert.Equal("1.1.0", published.Payload[0].Version);
        }

        [Fact]
        public async Task CurrentAndVersionedOpenApiSchemaRoutesAreReachable()
        {
            using var currentResponse = await _client.GetAsync("/v1/Glossary/schema");
            using var versionResponse = await _client.GetAsync(
                "/v1/Glossary/versions/1.0.0/schema"
            );
            using var missingVersionResponse = await _client.GetAsync(
                "/v1/Glossary/versions/9.9.9/schema"
            );

            currentResponse.EnsureSuccessStatusCode();
            versionResponse.EnsureSuccessStatusCode();
            Assert.Equal(TimeSpan.FromMinutes(5), currentResponse.Headers.CacheControl?.MaxAge);
            Assert.True(currentResponse.Headers.CacheControl?.Public);
            Assert.True(currentResponse.Headers.CacheControl?.MustRevalidate);
            Assert.Equal(TimeSpan.FromDays(365), versionResponse.Headers.CacheControl?.MaxAge);
            Assert.True(versionResponse.Headers.CacheControl?.Public);
            Assert.Contains(
                versionResponse.Headers.CacheControl?.Extensions ?? [],
                directive => directive.Name == "immutable"
            );
            Assert.Equal(
                "application/vnd.oai.openapi+json",
                currentResponse.Content.Headers.ContentType?.MediaType
            );

            using var currentDocument = JsonDocument.Parse(
                await currentResponse.Content.ReadAsStringAsync()
            );
            using var versionDocument = JsonDocument.Parse(
                await versionResponse.Content.ReadAsStringAsync()
            );

            Assert.Equal(
                "1.0.0",
                currentDocument.RootElement.GetProperty("info").GetProperty("version").GetString()
            );
            Assert.Equal(
                "Connected Services Semantics Glossary",
                currentDocument.RootElement
                    .GetProperty("info")
                    .GetProperty("title")
                    .GetString()
            );
            Assert.Equal(
                "1.0.0",
                versionDocument.RootElement.GetProperty("info").GetProperty("version").GetString()
            );
            Assert.False(
                versionDocument.RootElement
                    .GetProperty("components")
                    .GetProperty("schemas")
                    .GetProperty("access-control")
                    .TryGetProperty("x-bc-semantic-glossary-version", out _)
            );
            Assert.Equal(HttpStatusCode.NotFound, missingVersionResponse.StatusCode);
        }

        [Fact]
        public async Task TermVersionRoutesReturnPersistedHistory()
        {
            using var versionsResponse = await _client.GetAsync(
                "/v1/Glossary/terms/accuracy/versions"
            );
            using var versionResponse = await _client.GetAsync(
                "/v1/Glossary/terms/accuracy/versions/1"
            );
            using var missingResponse = await _client.GetAsync(
                "/v1/Glossary/terms/accuracy/versions/3"
            );

            versionsResponse.EnsureSuccessStatusCode();
            versionResponse.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);

            var versions = await versionsResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<IList<GlossaryModel>>
            >();
            var version = await versionResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryModel>
            >();

            Assert.Equal([2, 1], versions!.Payload.Select(item => item.Version));
            Assert.Equal(1, version!.Payload.Version);
            Assert.Equal("string", version.Payload.SchemaType);
        }

        [Fact]
        public async Task TermVersionSchemaRouteReturnsAnImmutableCanonicalRevision()
        {
            using var response = await _client.GetAsync(
                "/v1/Glossary/terms/accuracy/versions/1/schema"
            );
            using var missingResponse = await _client.GetAsync(
                "/v1/Glossary/terms/accuracy/versions/3/schema"
            );

            response.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.NotFound, missingResponse.StatusCode);
            Assert.Equal(TimeSpan.FromDays(365), response.Headers.CacheControl?.MaxAge);
            Assert.True(response.Headers.CacheControl?.Public);
            Assert.Contains(
                response.Headers.CacheControl?.Extensions ?? [],
                directive => directive.Name == "immutable"
            );
            Assert.NotNull(response.Headers.ETag);
            Assert.Equal(
                "application/json",
                response.Content.Headers.ContentType?.MediaType
            );

            using var document = JsonDocument.Parse(
                await response.Content.ReadAsStringAsync()
            );
            var schema = document.RootElement;
            Assert.Equal("string", schema.GetProperty("type").GetString());
            Assert.Equal("Accuracy", schema.GetProperty("title").GetString());
            Assert.Equal(
                "urn:bcgov:glossary:connected-services:accuracy",
                schema.GetProperty("x-bc-semantic-urn").GetString()
            );
            Assert.Equal(1, schema.GetProperty("x-bc-semantic-term-version").GetInt32());
            Assert.False(schema.TryGetProperty("x-bc-semantic-glossary-version", out _));
        }

        [Fact]
        public async Task EditingApiStagesAndPublishesOneGlossaryRelease()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            using var createResponse = await client.PostAsync("/v1/Glossary/drafts", null);
            var created = await createResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            Assert.NotNull(created);
            Assert.Equal("1.0.0", created.Payload.Draft.BaseVersion);
            Assert.Equal("1.0.1-alpha", created.Payload.Draft.Version);
            Assert.Equal("None", created.Payload.Draft.ChangeType);
            Assert.Equal(133, created.Payload.Terms.Count());

            var draftId = created.Payload.Draft.Id;
            var edit = new GlossaryTermEditModel
            {
                Term = "New Semantic Term",
                Definition = "A term added through the glossary editing API.",
                VerifiedDefinitionFlag = true,
                PublishToDevHub = true,
            };
            using var editResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/new-semantic-term",
                edit
            );
            var edited = await editResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryTermSubmissionResultModel>
            >();

            editResponse.EnsureSuccessStatusCode();
            Assert.NotNull(edited);
            Assert.True(edited.Payload.IsValid);
            Assert.True(Guid.TryParse(edited.Payload.Term!.StaticId, out _));

            using var previewResponse = await client.GetAsync($"/v1/Glossary/drafts/{draftId}");
            var preview = await previewResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();

            previewResponse.EnsureSuccessStatusCode();
            Assert.NotNull(preview);
            Assert.Equal("1.1.0-alpha", preview.Payload.Draft.Version);
            Assert.Equal("Minor", preview.Payload.Draft.ChangeType);
            Assert.Equal(134, preview.Payload.Terms.Count());
            using var unpublishedTermResponse = await client.GetAsync(
                "/v1/Glossary/new-semantic-term"
            );
            Assert.Equal(
                HttpStatusCode.NotFound,
                unpublishedTermResponse.StatusCode
            );

            using var publishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = draftId }
            );
            var published = await publishResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPublishResultModel>
            >();

            publishResponse.EnsureSuccessStatusCode();
            Assert.NotNull(published);
            Assert.Equal(HttpStatusCode.Created, publishResponse.StatusCode);
            Assert.Equal("Published", published.Payload.Status);
            Assert.Equal("1.1.0", published.Payload.Release!.Version);

            using var repeatedPublishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = draftId }
            );
            var repeated = await repeatedPublishResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPublishResultModel>
            >();
            using var publishedDraftResponse = await client.GetAsync(
                $"/v1/Glossary/drafts/{draftId}"
            );
            var publishedDraft = await publishedDraftResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            using var versionsResponse = await client.GetAsync("/v1/Glossary/versions");
            var versions = await versionsResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<IList<GlossaryVersionModel>>
            >();

            Assert.Equal(HttpStatusCode.Created, repeatedPublishResponse.StatusCode);
            Assert.Equal(publishResponse.Headers.Location, repeatedPublishResponse.Headers.Location);
            Assert.Equal("1.1.0", repeated!.Payload.Release!.Version);
            Assert.Equal("Published", publishedDraft!.Payload.Draft.Status);
            Assert.Equal("1.1.0", publishedDraft.Payload.Draft.Version);
            Assert.Single(versions!.Payload, item => item.Version == "1.1.0");

            using var reusedTermResponse = await client.GetAsync("/v1/Glossary/accuracy");
            var reusedTerm = await reusedTermResponse.Content.ReadFromJsonAsync<
                GlossaryResponseModel<GlossaryModel>
            >();
            reusedTermResponse.EnsureSuccessStatusCode();
            Assert.Equal(2, reusedTerm!.Payload.Version);

            using var termResponse = await client.GetAsync("/v1/Glossary/new-semantic-term");
            var term = await termResponse.Content.ReadFromJsonAsync<
                GlossaryResponseModel<GlossaryModel>
            >();

            termResponse.EnsureSuccessStatusCode();
            Assert.NotNull(term);
            Assert.Equal(1, term.Payload.Version);
            Assert.Equal("1.1.0", term.Glossary.Version);
        }

        [Fact]
        public async Task UnpublishedTermIsRetainedForDirectLookupButExcludedFromOutputs()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            using var createResponse = await client.PostAsync("/v1/Glossary/drafts", null);
            var created = await createResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var draftId = created!.Payload.Draft.Id;
            var edit = new GlossaryTermEditModel
            {
                Term = "Internal Review Term",
                Definition = "A valid term that is not approved for published outputs.",
                VerifiedDefinitionFlag = false,
                PublishToDevHub = false,
            };

            using var editResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/internal-review-term",
                edit
            );
            var edited = await editResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryTermSubmissionResultModel>
            >();
            using var publishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = draftId }
            );
            var published = await publishResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPublishResultModel>
            >();

            editResponse.EnsureSuccessStatusCode();
            publishResponse.EnsureSuccessStatusCode();
            var termId = Guid.Parse(edited!.Payload.Term!.StaticId);
            var glossaryVersion = published!.Payload.Release!.Version;

            var currentBySlug = await client.GetFromJsonAsync<
                GlossaryResponseModel<GlossaryModel>
            >("/v1/Glossary/internal-review-term");
            var currentById = await client.GetFromJsonAsync<
                GlossaryResponseModel<GlossaryModel>
            >($"/v1/Glossary/id/{termId}");
            var versionBySlug = await client.GetFromJsonAsync<
                GlossaryResponseModel<GlossaryModel>
            >($"/v1/Glossary/versions/{glossaryVersion}/terms/internal-review-term");
            var currentList = await client.GetFromJsonAsync<
                GlossaryResponseModel<IEnumerable<GlossaryModel>>
            >("/v1/Glossary");
            var versionList = await client.GetFromJsonAsync<
                GlossaryResponseModel<IEnumerable<GlossaryModel>>
            >($"/v1/Glossary/versions/{glossaryVersion}");
            var markdown = await client.GetStringAsync("/v1/Glossary/markdown");
            using var schema = JsonDocument.Parse(
                await client.GetStringAsync("/v1/Glossary/schema")
            );
            using var termSchemaResponse = await client.GetAsync(
                "/v1/Glossary/terms/internal-review-term/versions/1/schema"
            );

            Assert.False(currentBySlug!.Payload.VerifiedDefinitionFlag);
            Assert.False(currentBySlug.Payload.PublishToDevHub);
            Assert.Equal(termId.ToString(), currentById!.Payload.StaticId);
            Assert.False(currentById.Payload.VerifiedDefinitionFlag);
            Assert.False(currentById.Payload.PublishToDevHub);
            Assert.False(versionBySlug!.Payload.VerifiedDefinitionFlag);
            Assert.False(versionBySlug.Payload.PublishToDevHub);
            Assert.DoesNotContain(
                currentList!.Payload,
                item => item.Name == "internal-review-term"
            );
            Assert.DoesNotContain(
                versionList!.Payload,
                item => item.Name == "internal-review-term"
            );
            Assert.DoesNotContain("Internal Review Term", markdown, StringComparison.Ordinal);
            Assert.False(
                schema
                    .RootElement.GetProperty("components")
                    .GetProperty("schemas")
                    .TryGetProperty("internal-review-term", out _)
            );
            Assert.Equal(HttpStatusCode.NotFound, termSchemaResponse.StatusCode);
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public async Task RemovingPublishedTermFromGeneratedOutputsProducesMajorRelease(
            bool verified,
            bool published
        )
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            var initial = await client.GetFromJsonAsync<GlossaryResponseModel<GlossaryModel>>(
                "/v1/Glossary/access-control"
            );
            using var createResponse = await client.PostAsync("/v1/Glossary/drafts", null);
            var created = await createResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var edit = ToEdit(initial!.Payload);
            edit.VerifiedDefinitionFlag = verified;
            edit.PublishToDevHub = published;

            using var editResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{created!.Payload.Draft.Id}/terms/access-control",
                edit
            );
            using var previewResponse = await client.GetAsync(
                $"/v1/Glossary/drafts/{created.Payload.Draft.Id}"
            );
            var preview = await previewResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            using var publishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = created.Payload.Draft.Id }
            );
            var result = await publishResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPublishResultModel>
            >();
            using var schema = JsonDocument.Parse(
                await client.GetStringAsync("/v1/Glossary/schema")
            );

            editResponse.EnsureSuccessStatusCode();
            previewResponse.EnsureSuccessStatusCode();
            publishResponse.EnsureSuccessStatusCode();
            Assert.Equal("2.0.0-alpha", preview!.Payload.Draft.Version);
            Assert.Equal("Major", preview.Payload.Draft.ChangeType);
            Assert.Equal("2.0.0", result!.Payload.Release!.Version);
            Assert.False(
                schema
                    .RootElement.GetProperty("components")
                    .GetProperty("schemas")
                    .TryGetProperty("access-control", out _)
            );
        }

        [Fact]
        public async Task InvalidEditIsAuditedWithoutChangingTheDraft()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            using var createResponse = await client.PostAsync("/v1/Glossary/drafts", null);
            var created = await createResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var draftId = created!.Payload.Draft.Id;
            var edit = new GlossaryTermEditModel
            {
                Term = "Access Control",
                Definition = "",
                VerifiedDefinitionFlag = true,
                PublishToDevHub = true,
            };

            using var editResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/access-control",
                edit
            );
            var result = await editResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryTermSubmissionResultModel>
            >();

            Assert.Equal(HttpStatusCode.UnprocessableEntity, editResponse.StatusCode);
            Assert.NotNull(result);
            Assert.False(result.Payload.IsValid);
            Assert.Contains("Published Definition is required", result.Payload.InvalidReasons);

            using var previewResponse = await client.GetAsync($"/v1/Glossary/drafts/{draftId}");
            var preview = await previewResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            Assert.Equal("1.0.1-alpha", preview!.Payload.Draft.Version);
            Assert.Equal("None", preview.Payload.Draft.ChangeType);
            Assert.Equal(133, preview.Payload.Terms.Count());

            using var scope = application.Services.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<
                IDbContextFactory<GlossaryDbContext>
            >();
            using var context = contextFactory.CreateDbContext();
            var submission = Assert.Single(
                context.GlossaryTermSubmissions.Where(item => item.DraftId == draftId)
            );
            Assert.False(submission.IsValid);
            Assert.Null(submission.TermRevisionId);
            Assert.Equal("Upsert", submission.Operation);
        }

        [Fact]
        public async Task DraftPreviewClassifiesChangesAndProjectsTermVersions()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            using var createResponse = await client.PostAsync("/v1/Glossary/drafts", null);
            var created = await createResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var draftId = created!.Payload.Draft.Id;
            var accessControl = created.Payload.Terms.Single(item =>
                item.Name == "access-control"
            );
            var accuracy = created.Payload.Terms.Single(item => item.Name == "accuracy");
            var accessControlEdit = ToEdit(accessControl);
            accessControlEdit.Definition += " Clarified for this draft.";
            var accuracyEdit = ToEdit(accuracy);
            accuracyEdit.Definition += " This change is explicitly incompatible.";
            accuracyEdit.BreakingChange = true;

            using var newResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/new-semantic-term",
                new GlossaryTermEditModel
                {
                    Term = "New Semantic Term",
                    Definition = "A term added through the glossary editing API.",
                    VerifiedDefinitionFlag = true,
                    PublishToDevHub = true,
                }
            );
            using var bugfixResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/access-control",
                accessControlEdit
            );
            using var breakingResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/accuracy",
                accuracyEdit
            );
            using var deleteResponse = await client.DeleteAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/applicant"
            );

            newResponse.EnsureSuccessStatusCode();
            bugfixResponse.EnsureSuccessStatusCode();
            breakingResponse.EnsureSuccessStatusCode();
            deleteResponse.EnsureSuccessStatusCode();

            using var previewResponse = await client.GetAsync(
                $"/v1/Glossary/drafts/{draftId}"
            );
            var preview = await previewResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            previewResponse.EnsureSuccessStatusCode();
            var changes = preview!.Payload.TermChanges.ToDictionary(item => item.Term.Name);

            Assert.Equal("New", changes["new-semantic-term"].ChangeType);
            Assert.False(changes["new-semantic-term"].BreakingChange);
            Assert.Equal(1, changes["new-semantic-term"].Term.Version);
            Assert.Null(changes["new-semantic-term"].OriginalTerm);
            Assert.Equal("Bugfix", changes["access-control"].ChangeType);
            Assert.False(changes["access-control"].BreakingChange);
            Assert.Equal(2, changes["access-control"].Term.Version);
            Assert.Equal(
                accessControl.Definition,
                changes["access-control"].OriginalTerm!.Definition
            );
            Assert.Equal("Breaking Change", changes["accuracy"].ChangeType);
            Assert.True(changes["accuracy"].BreakingChange);
            Assert.Equal(3, changes["accuracy"].Term.Version);
            Assert.Equal(accuracy.Definition, changes["accuracy"].OriginalTerm!.Definition);
            Assert.Equal("Deleted", changes["applicant"].ChangeType);
            Assert.False(changes["applicant"].BreakingChange);
            Assert.Equal(1, changes["applicant"].Term.Version);
            Assert.Equal(
                changes["applicant"].Term.StaticId,
                changes["applicant"].OriginalTerm!.StaticId
            );
            Assert.Equal(
                1,
                preview.Payload.Terms.Single(item => item.Name == "api-catalogue").Version
            );
            Assert.DoesNotContain(
                preview.Payload.Terms,
                item => item.Name == "applicant"
            );

            using var restoreChangedResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/access-control",
                ToEdit(changes["access-control"].OriginalTerm!)
            );
            using var restoreDeletedResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/applicant",
                ToEdit(changes["applicant"].OriginalTerm!)
            );
            restoreChangedResponse.EnsureSuccessStatusCode();
            restoreDeletedResponse.EnsureSuccessStatusCode();

            using var restoredPreviewResponse = await client.GetAsync(
                $"/v1/Glossary/drafts/{draftId}"
            );
            var restoredPreview = await restoredPreviewResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            restoredPreviewResponse.EnsureSuccessStatusCode();
            Assert.DoesNotContain(
                restoredPreview!.Payload.TermChanges,
                item => item.Term.Name is "access-control" or "applicant"
            );
            Assert.Equal(
                accessControl.Definition,
                restoredPreview.Payload.Terms.Single(item =>
                    item.Name == "access-control"
                ).Definition
            );
            Assert.Contains(
                restoredPreview.Payload.Terms,
                item => item.Name == "applicant"
            );
        }

        [Fact]
        public async Task PublishRejectsUnresolvedInvalidTermsUnlessTheyAreIgnored()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            using var createResponse = await client.PostAsync("/v1/Glossary/drafts", null);
            var created = await createResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var draftId = created!.Payload.Draft.Id;
            var validEdit = new GlossaryTermEditModel
            {
                Term = "New Semantic Term",
                Definition = "A valid addition.",
                VerifiedDefinitionFlag = true,
                PublishToDevHub = true,
            };
            var invalidEdit = new GlossaryTermEditModel
            {
                Term = "Access Control",
                Definition = "",
                VerifiedDefinitionFlag = true,
                PublishToDevHub = true,
            };

            using var validResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/new-semantic-term",
                validEdit
            );
            using var invalidResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/access-control",
                invalidEdit
            );
            using var blockedResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = draftId }
            );
            var blocked = await blockedResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPublishResultModel>
            >();

            validResponse.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidResponse.StatusCode);
            Assert.Equal(HttpStatusCode.UnprocessableEntity, blockedResponse.StatusCode);
            Assert.Equal("Invalid", blocked!.Payload.Status);
            var invalidTerm = Assert.Single(blocked.Payload.InvalidTerms);
            Assert.Equal("access-control", invalidTerm.Name);
            Assert.Contains("Published Definition is required", invalidTerm.InvalidReasons);

            using var publishedResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel
                {
                    DraftId = draftId,
                    IgnoreInvalid = true,
                }
            );
            var published = await publishedResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPublishResultModel>
            >();

            publishedResponse.EnsureSuccessStatusCode();
            Assert.Equal("Published", published!.Payload.Status);
            Assert.Equal("1.1.0", published.Payload.Release!.Version);
        }

        [Fact]
        public async Task ExplicitBreakingTermEditProducesAMajorRelease()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            using var createResponse = await client.PostAsync("/v1/Glossary/drafts", null);
            var created = await createResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var draftId = created!.Payload.Draft.Id;
            var edit = new GlossaryTermEditModel
            {
                Term = "Access Control",
                Definition = "A deliberately incompatible definition.",
                VerifiedDefinitionFlag = true,
                PublishToDevHub = true,
                BreakingChange = true,
            };

            using var editResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/access-control",
                edit
            );
            using var previewResponse = await client.GetAsync($"/v1/Glossary/drafts/{draftId}");
            var preview = await previewResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();

            editResponse.EnsureSuccessStatusCode();
            previewResponse.EnsureSuccessStatusCode();
            Assert.Equal("2.0.0-alpha", preview!.Payload.Draft.Version);
            Assert.Equal("Major", preview.Payload.Draft.ChangeType);

            using var publishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = draftId }
            );
            var published = await publishResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPublishResultModel>
            >();

            publishResponse.EnsureSuccessStatusCode();
            Assert.Equal("2.0.0", published!.Payload.Release!.Version);
        }

        [Fact]
        public async Task PublishingAnotherReleaseMakesAnOlderDraftStale()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            using var firstCreateResponse = await client.PostAsync(
                "/v1/Glossary/drafts",
                null
            );
            using var secondCreateResponse = await client.PostAsync(
                "/v1/Glossary/drafts",
                null
            );
            var first = await firstCreateResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var second = await secondCreateResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var edit = new GlossaryTermEditModel
            {
                Term = "New Semantic Term",
                Definition = "A term added through the glossary editing API.",
                VerifiedDefinitionFlag = true,
                PublishToDevHub = true,
            };

            using var editResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{first!.Payload.Draft.Id}/terms/new-semantic-term",
                edit
            );
            using var firstPublishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = first.Payload.Draft.Id }
            );
            using var previewResponse = await client.GetAsync(
                $"/v1/Glossary/drafts/{second!.Payload.Draft.Id}"
            );
            var preview = await previewResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            using var stalePublishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = second.Payload.Draft.Id }
            );
            var stale = await stalePublishResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPublishResultModel>
            >();

            editResponse.EnsureSuccessStatusCode();
            firstPublishResponse.EnsureSuccessStatusCode();
            previewResponse.EnsureSuccessStatusCode();
            Assert.Equal("Stale", preview!.Payload.Draft.Status);
            Assert.Equal(HttpStatusCode.Conflict, stalePublishResponse.StatusCode);
            Assert.Equal("Stale", stale!.Payload.Status);
        }

        [Fact]
        public async Task RebaseAppliesAStaleDraftsChangesToTheCurrentRelease()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            using var firstCreateResponse = await client.PostAsync(
                "/v1/Glossary/drafts",
                null
            );
            using var secondCreateResponse = await client.PostAsync(
                "/v1/Glossary/drafts",
                null
            );
            var first = await firstCreateResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var second = await secondCreateResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var secondAccessControl = second!.Payload.Terms.Single(item =>
                item.Name == "access-control"
            );
            var secondEdit = ToEdit(secondAccessControl);
            secondEdit.Definition = "Access control changed in the second draft.";

            using var firstEditResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{first!.Payload.Draft.Id}/terms/new-semantic-term",
                new GlossaryTermEditModel
                {
                    Term = "New Semantic Term",
                    Definition = "A term added by the first draft.",
                    VerifiedDefinitionFlag = true,
                    PublishToDevHub = true,
                }
            );
            using var secondEditResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{second.Payload.Draft.Id}/terms/access-control",
                secondEdit
            );
            using var firstPublishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = first.Payload.Draft.Id }
            );
            using var rebaseResponse = await client.PostAsync(
                $"/v1/Glossary/drafts/{second.Payload.Draft.Id}/rebase",
                null
            );
            var rebased = await rebaseResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftRebaseResultModel>
            >();

            firstEditResponse.EnsureSuccessStatusCode();
            secondEditResponse.EnsureSuccessStatusCode();
            firstPublishResponse.EnsureSuccessStatusCode();
            rebaseResponse.EnsureSuccessStatusCode();
            Assert.Equal("Rebased", rebased!.Payload.Status);
            Assert.Equal("Draft", rebased.Payload.Preview!.Draft.Status);
            Assert.Equal("1.1.0", rebased.Payload.Preview.Draft.BaseVersion);
            Assert.Contains(
                rebased.Payload.Preview.Terms,
                item => item.Name == "new-semantic-term"
            );
            Assert.Equal(
                secondEdit.Definition,
                rebased.Payload.Preview.Terms.Single(item =>
                    item.Name == "access-control"
                ).Definition
            );
            Assert.DoesNotContain(
                rebased.Payload.Preview.TermChanges,
                item => item.Term.Name == "new-semantic-term"
            );
            Assert.Equal(
                "Bugfix",
                Assert.Single(rebased.Payload.Preview.TermChanges).ChangeType
            );
        }

        [Fact]
        public async Task RebaseReportsTermsChangedDifferentlyInBothDrafts()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            using var firstCreateResponse = await client.PostAsync(
                "/v1/Glossary/drafts",
                null
            );
            using var secondCreateResponse = await client.PostAsync(
                "/v1/Glossary/drafts",
                null
            );
            var first = await firstCreateResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var second = await secondCreateResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var firstEdit = ToEdit(
                first!.Payload.Terms.Single(item => item.Name == "access-control")
            );
            firstEdit.Definition = "The definition from the first draft.";
            var secondEdit = ToEdit(
                second!.Payload.Terms.Single(item => item.Name == "access-control")
            );
            secondEdit.Definition = "The definition from the second draft.";

            using var firstEditResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{first.Payload.Draft.Id}/terms/access-control",
                firstEdit
            );
            using var secondEditResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{second.Payload.Draft.Id}/terms/access-control",
                secondEdit
            );
            using var firstPublishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = first.Payload.Draft.Id }
            );
            using var rebaseResponse = await client.PostAsync(
                $"/v1/Glossary/drafts/{second.Payload.Draft.Id}/rebase",
                null
            );
            var result = await rebaseResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftRebaseResultModel>
            >();

            firstEditResponse.EnsureSuccessStatusCode();
            secondEditResponse.EnsureSuccessStatusCode();
            firstPublishResponse.EnsureSuccessStatusCode();
            Assert.Equal(HttpStatusCode.Conflict, rebaseResponse.StatusCode);
            Assert.Equal("Conflict", result!.Payload.Status);
            Assert.Equal(["access-control"], result.Payload.ConflictingTerms);

            using var stalePreviewResponse = await client.GetAsync(
                $"/v1/Glossary/drafts/{second.Payload.Draft.Id}"
            );
            var stalePreview = await stalePreviewResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            Assert.Equal("Stale", stalePreview!.Payload.Draft.Status);
        }

        [Fact]
        public async Task RepeatedDraftEditsProduceOnePublishedTermRevision()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            using var createResponse = await client.PostAsync("/v1/Glossary/drafts", null);
            var created = await createResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var draftId = created!.Payload.Draft.Id;
            var edit = new GlossaryTermEditModel
            {
                Term = "Access Control",
                Definition = "First draft definition.",
                VerifiedDefinitionFlag = true,
                PublishToDevHub = true,
            };

            using var firstEditResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/access-control",
                edit
            );
            edit.Definition = "Final draft definition.";
            using var finalEditResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{draftId}/terms/access-control",
                edit
            );
            using var beforePublishResponse = await client.GetAsync(
                "/v1/Glossary/terms/access-control/versions"
            );
            var beforePublish = await beforePublishResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<IList<GlossaryModel>>
            >();

            firstEditResponse.EnsureSuccessStatusCode();
            finalEditResponse.EnsureSuccessStatusCode();
            Assert.Equal([1], beforePublish!.Payload.Select(item => item.Version));

            using var publishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = draftId }
            );
            using var afterPublishResponse = await client.GetAsync(
                "/v1/Glossary/terms/access-control/versions"
            );
            var afterPublish = await afterPublishResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<IList<GlossaryModel>>
            >();

            publishResponse.EnsureSuccessStatusCode();
            Assert.Equal([2, 1], afterPublish!.Payload.Select(item => item.Version));
            Assert.Equal("Final draft definition.", afterPublish.Payload[0].Definition);

            using var scope = application.Services.CreateScope();
            var contextFactory = scope.ServiceProvider.GetRequiredService<
                IDbContextFactory<GlossaryDbContext>
            >();
            using var context = contextFactory.CreateDbContext();
            var submissions = context
                .GlossaryTermSubmissions.Where(item => item.DraftId == draftId)
                .OrderBy(item => item.Sequence)
                .ToList();
            Assert.Equal(2, submissions.Count);
            Assert.Null(submissions[0].TermRevisionId);
            Assert.NotNull(submissions[1].TermRevisionId);
        }

        [Fact]
        public async Task RestoredTermUsesItsPersistedRevisionInDraftAndPublishedPreviews()
        {
            using var application = new GlossaryApiFactory();
            using var client = application.CreateClient();
            var original = await client.GetFromJsonAsync<GlossaryResponseModel<GlossaryModel>>(
                "/v1/Glossary/access-control"
            );

            using var deleteDraftResponse = await client.PostAsync(
                "/v1/Glossary/drafts",
                null
            );
            var deleteDraft = await deleteDraftResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            using var deleteResponse = await client.DeleteAsync(
                $"/v1/Glossary/drafts/{deleteDraft!.Payload.Draft.Id}/terms/access-control"
            );
            using var deletePublishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = deleteDraft.Payload.Draft.Id }
            );

            deleteResponse.EnsureSuccessStatusCode();
            deletePublishResponse.EnsureSuccessStatusCode();

            using var restoreDraftResponse = await client.PostAsync(
                "/v1/Glossary/drafts",
                null
            );
            var restoreDraft = await restoreDraftResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();
            var restoreDraftId = restoreDraft!.Payload.Draft.Id;
            using var restoreResponse = await client.PutAsJsonAsync(
                $"/v1/Glossary/drafts/{restoreDraftId}/terms/access-control",
                ToEdit(original!.Payload)
            );
            using var previewResponse = await client.GetAsync(
                $"/v1/Glossary/drafts/{restoreDraftId}"
            );
            var preview = await previewResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();

            restoreResponse.EnsureSuccessStatusCode();
            previewResponse.EnsureSuccessStatusCode();
            Assert.Equal(
                2,
                preview!.Payload.Terms.Single(item => item.Name == "access-control").Version
            );
            Assert.Equal(
                2,
                preview.Payload.TermChanges.Single(item =>
                    item.Term.Name == "access-control"
                ).Term.Version
            );

            using var publishResponse = await client.PostAsJsonAsync(
                "/v1/Glossary/versions",
                new GlossaryVersionPublishModel { DraftId = restoreDraftId }
            );
            using var publishedPreviewResponse = await client.GetAsync(
                $"/v1/Glossary/drafts/{restoreDraftId}"
            );
            var publishedPreview = await publishedPreviewResponse.Content.ReadFromJsonAsync<
                BaseResponseModel<GlossaryDraftPreviewModel>
            >();

            publishResponse.EnsureSuccessStatusCode();
            publishedPreviewResponse.EnsureSuccessStatusCode();
            Assert.Equal("Published", publishedPreview!.Payload.Draft.Status);
            Assert.Equal(
                2,
                publishedPreview.Payload.Terms.Single(item =>
                    item.Name == "access-control"
                ).Version
            );
            Assert.Equal(
                2,
                publishedPreview.Payload.TermChanges.Single(item =>
                    item.Term.Name == "access-control"
                ).Term.Version
            );
        }

        [Fact]
        public void ApplicationStartupFailsWhenTheCurrentGlossaryCannotLoad()
        {
            using var baseApplication = new GlossaryApiFactory();
            using var application = baseApplication.WithWebHostBuilder(
                builder =>
                    builder.ConfigureServices(services =>
                    {
                        services.RemoveAll<IGlossaryProvider>();
                        services.AddSingleton<IGlossaryProvider>(_ =>
                            throw new InvalidDataException("Invalid current glossary.")
                        );
                    })
            );

            var exception = Assert.ThrowsAny<Exception>(() => application.CreateClient());

            Assert.Contains("Invalid current glossary", exception.ToString());
        }

        private static void AssertAnonymous(JsonElement operation)
        {
            var requirement = Assert.Single(
                operation.GetProperty("security").EnumerateArray()
            );
            Assert.Empty(requirement.EnumerateObject());
        }

        private static GlossaryTermEditModel ToEdit(GlossaryModel term)
        {
            return new GlossaryTermEditModel
            {
                StaticId = term.StaticId,
                Term = term.Term,
                Definition = term.Definition,
                Example = term.Example,
                SchemaType = term.SchemaType,
                SchemaConstraints = JsonSerializer.Serialize(term.SchemaConstraints),
                Keywords = term.Keywords,
                Scope = term.Scope,
                ScopeUrl = term.ScopeUrl,
                Citations = term.Citations,
                TeamSource = term.TeamSource,
                VerifiedDefinitionFlag = term.VerifiedDefinitionFlag,
                PublishToDevHub = term.PublishToDevHub,
            };
        }
    }
}
