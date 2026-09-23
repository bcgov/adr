namespace Adr.Semantics.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.Json;
    using Adr.Semantics.Data;
    using Adr.Semantics.Models;
    using Adr.Semantics.Providers;
    using Microsoft.Extensions.Logging;

    public class GlossaryProviderTests
    {
        [Fact]
        public void InvalidTermIsLoggedAndLaterImportsStillUseTheAcceptedTerms()
        {
            var logger = new CapturingLogger<GlossaryTermProcessor>();
            using var database = new GlossaryTestDatabase(termLogger: logger);
            var releases = database.Provider.GetVersions().ToList();

            Assert.Equal(["1.0.0", "0.1.0"], releases.Select(item => item.Version));
            Assert.Equal(132, database.Provider.GetAllGlossaries("0.1.0")!.Count());
            Assert.Equal(133, database.Provider.GetAllGlossaries("1.0.0")!.Count());
            Assert.Contains(
                logger.Entries,
                entry =>
                    entry.Level == LogLevel.Critical
                    && entry.Message.Contains("SUBMISSION 114", StringComparison.Ordinal)
                    && entry.Message.Contains("spouse", StringComparison.Ordinal)
                    && entry.Message.Contains("WILL NOT BE PUBLISHED", StringComparison.Ordinal)
            );
        }

        [Fact]
        public void InvalidSubmissionIsRetainedWithoutCreatingATermRevision()
        {
            using var database = new GlossaryTestDatabase();
            using var context = database.Factory.CreateDbContext();

            var invalid = Assert.Single(
                context.GlossaryTermSubmissions.Where(item => !item.IsValid)
            );

            Assert.Equal("spouse", invalid.Name);
            Assert.Null(invalid.TermRevisionId);
            Assert.NotNull(invalid.ResolvedStaticId);
            Assert.Contains(
                "already accepted",
                JsonSerializer.Deserialize<List<string>>(invalid.InvalidReasonsJson)!.Single(),
                StringComparison.OrdinalIgnoreCase
            );
        }

        [Fact]
        public void InvalidUpdateDoesNotChangeTheEffectiveGlossaryOrVersions()
        {
            using var database = new GlossaryTestDatabase();
            var current = new GlossaryModel
            {
                StaticId = "11111111-1111-1111-1111-111111111111",
                Name = "existing-term",
                Term = "Existing Term",
                Definition = "Last valid definition",
                VerifiedDefinitionFlag = true,
                PublishToDevHub = true,
            };
            var invalidUpdate = new GlossaryModel
            {
                Name = current.Name,
                Term = current.Term,
                Definition = "",
                VerifiedDefinitionFlag = true,
                PublishToDevHub = true,
            };

            var result = database.Processor.Process(
                [GlossaryTermSubmission.FromTerm(1, invalidUpdate)],
                [current],
                "editing-api"
            );

            var attempt = Assert.Single(result.Attempts);
            Assert.False(attempt.IsValid);
            Assert.Equal(Guid.Parse(current.StaticId), attempt.ResolvedStaticId);
            Assert.Equal(current, Assert.Single(result.EffectiveTerms));
            Assert.Equal(
                GlossaryChangeKind.None,
                GlossaryChangeAnalyzer.Analyze([current], result.EffectiveTerms)
            );
        }

        [Fact]
        public void ValidUnpublishedTermsRemainInTheEffectiveGlossary()
        {
            using var database = new GlossaryTestDatabase();
            var published = new GlossaryModel
            {
                Name = "published-term",
                Term = "Published Term",
                Definition = "Published definition",
                VerifiedDefinitionFlag = true,
                PublishToDevHub = true,
            };
            var unpublished = new GlossaryModel
            {
                Name = "unpublished-term",
                Term = "Unpublished Term",
                Definition = "Unpublished definition",
                VerifiedDefinitionFlag = false,
                PublishToDevHub = false,
            };

            var result = database.Processor.Process(
                [
                    GlossaryTermSubmission.FromTerm(1, published),
                    GlossaryTermSubmission.FromTerm(2, unpublished),
                ],
                [],
                "editing-api"
            );

            Assert.Equal(
                ["published-term", "unpublished-term"],
                result.EffectiveTerms.Select(item => item.Name)
            );
        }

        [Fact]
        public void ImportsArePersistedAndNotReappliedAtStartup()
        {
            using var database = new GlossaryTestDatabase();

            database.Importer.Initialize();

            Assert.Equal(2, database.Provider.GetVersions().Count());
        }

        [Fact]
        public void OnlyChangedImportRowsCreateATermVersion()
        {
            using var database = new GlossaryTestDatabase();

            var accessControl = database.Provider.GetTermVersions("access-control")!.ToList();
            var accuracy = database.Provider.GetTermVersions("accuracy")!.ToList();

            Assert.Equal([1], accessControl.Select(item => item.Version));
            Assert.Equal([2, 1], accuracy.Select(item => item.Version));
            Assert.Equal("number", accuracy[0].SchemaType);
            Assert.Equal("string", accuracy[1].SchemaType);
        }

        [Fact]
        public void BreakingChangeColumnIsOptionalAndParsedWhenProvided()
        {
            var csv = """
                Name,Term,Published Definition,Keywords,Scope,Scope URL,Citations,Team Source - Temp,Verified Definition,Publish to DevHub,Breaking Change
                breaking-term,Breaking Term,Definition,,,,,,Yes,Yes,Yes
                normal-term,Normal Term,Definition,,,,,,Yes,Yes,
                """;

            var submissions = GlossaryImportService.LoadSubmissions(Encoding.UTF8.GetBytes(csv));

            Assert.True(submissions[0].BreakingChange);
            Assert.False(submissions[1].BreakingChange);
        }

        [Fact]
        public void InvalidBreakingChangeValueIsLoggedAndIgnored()
        {
            var logger = new CapturingLogger<GlossaryTermProcessor>();
            using var database = new GlossaryTestDatabase(termLogger: logger);
            var csv = """
                Name,Term,Published Definition,Keywords,Scope,Scope URL,Citations,Team Source - Temp,Verified Definition,Publish to DevHub,Breaking Change
                invalid-term,Invalid Term,Definition,,,,,,Yes,Yes,Maybe
                valid-term,Valid Term,Definition,,,,,,Yes,Yes,No
                """;

            var submissions = GlossaryImportService.LoadSubmissions(Encoding.UTF8.GetBytes(csv));
            var processed = database.Processor.Process(submissions, [], "example.csv");

            Assert.Single(processed.EffectiveTerms);
            Assert.False(processed.Attempts[0].IsValid);
            Assert.Contains(
                logger.Entries,
                entry =>
                    entry.Level == LogLevel.Critical
                    && entry.Message.Contains("SUBMISSION 1", StringComparison.Ordinal)
                    && entry.Message.Contains("WILL NOT BE PUBLISHED", StringComparison.Ordinal)
            );
        }

        [Fact]
        public void MissingStaticIdIsReusedForAnUpdateAndGeneratedForANewTerm()
        {
            using var database = new GlossaryTestDatabase();
            var current = new GlossaryModel
            {
                StaticId = "11111111-1111-1111-1111-111111111111",
                Name = "existing-term",
                Term = "Existing Term",
                Definition = "Existing definition",
            };
            var updated = new GlossaryModel
            {
                Name = "existing-term",
                Term = "Existing Term",
                Definition = "Updated definition",
            };
            var added = new GlossaryModel
            {
                Name = "new-term",
                Term = "New Term",
                Definition = "New definition",
            };

            database.Processor.Process(
                [
                    GlossaryTermSubmission.FromTerm(1, updated),
                    GlossaryTermSubmission.FromTerm(2, added),
                ],
                [],
                "editing-api",
                [current]
            );

            Assert.Equal(current.StaticId, updated.StaticId);
            Assert.True(Guid.TryParse(added.StaticId, out _));
            Assert.NotEqual(current.StaticId, added.StaticId);
        }

        [Fact]
        public void UnreadableRecordIsRetainedWithoutRejectingFollowingRecords()
        {
            var logger = new CapturingLogger<GlossaryTermProcessor>();
            using var database = new GlossaryTestDatabase(termLogger: logger);
            var csv = """
                Name,Term,Published Definition,Keywords,Scope,Scope URL,Citations,Team Source - Temp,Verified Definition,Publish to DevHub
                invalid-term,Invalid Term,Definition,,,,,,Maybe,Yes
                valid-term,Valid Term,Definition,,,,,,Yes,Yes
                """;

            var submissions = GlossaryImportService.LoadSubmissions(Encoding.UTF8.GetBytes(csv));

            var processed = database.Processor.Process(submissions, [], "example.csv");
            var term = Assert.Single(processed.AcceptedTerms);
            var invalid = Assert.Single(processed.Attempts, item => !item.IsValid);
            Assert.Equal("valid-term", term.Name);
            Assert.Equal(2, term.SourceRecordNumber);
            Assert.Equal("invalid-term", invalid.Name);
            Assert.NotEmpty(invalid.InvalidReasons);
            Assert.Contains(
                logger.Entries,
                entry =>
                    entry.Level == LogLevel.Critical
                    && entry.Message.Contains("SUBMISSION 1", StringComparison.Ordinal)
                    && entry.Message.Contains("COULD NOT BE READ", StringComparison.OrdinalIgnoreCase)
            );
        }

        [Fact]
        public void UnsupportedSchemaTypeIsLoggedAndFallsBackToString()
        {
            var logger = new CapturingLogger<GlossaryTermProcessor>();
            using var database = new GlossaryTestDatabase(termLogger: logger);
            var term = new GlossaryModel
            {
                Term = "Example Term",
                Name = "example-term",
                SchemaType = "object",
            };

            database.Processor.NormalizeSchemaMetadata(term, "example.csv");

            Assert.Equal("string", term.SchemaType);
            Assert.Contains(
                logger.Entries,
                entry =>
                    entry.Level == LogLevel.Warning
                    && entry.Message.Contains("UNSUPPORTED SCHEMA TYPE object", StringComparison.Ordinal)
                    && entry.Message.Contains("example-term", StringComparison.Ordinal)
                    && entry.Message.Contains("example.csv", StringComparison.Ordinal)
                    && entry.Message.Contains("FALLING BACK TO STRING", StringComparison.Ordinal)
            );
        }

        [Fact]
        public void InvalidSchemaConstraintsAreLoggedAndExcludedIndividually()
        {
            var logger = new CapturingLogger<GlossaryTermProcessor>();
            using var database = new GlossaryTestDatabase(termLogger: logger);
            var term = new GlossaryModel
            {
                Term = "Example Term",
                Name = "example-term",
                SchemaType = "number",
                SchemaConstraintsSource =
                    """{"minimum":0,"maximum":"100","minLength":2,"multipleOf":0,"format":"double"}""",
            };

            database.Processor.NormalizeSchemaMetadata(term, "example.csv");

            Assert.Equal(2, term.SchemaConstraints.Count);
            Assert.Contains("minimum", term.SchemaConstraints.Keys);
            Assert.Contains("format", term.SchemaConstraints.Keys);
            Assert.DoesNotContain("maximum", term.SchemaConstraints.Keys);
            Assert.DoesNotContain("minLength", term.SchemaConstraints.Keys);
            Assert.DoesNotContain("multipleOf", term.SchemaConstraints.Keys);
            AssertInvalidConstraintLogged(logger, "maximum");
            AssertInvalidConstraintLogged(logger, "minLength");
            AssertInvalidConstraintLogged(logger, "multipleOf");
        }

        [Fact]
        public void InvalidSchemaConstraintsDocumentIsLoggedAndIgnored()
        {
            var logger = new CapturingLogger<GlossaryTermProcessor>();
            using var database = new GlossaryTestDatabase(termLogger: logger);
            var term = new GlossaryModel
            {
                Term = "Example Term",
                Name = "example-term",
                SchemaConstraintsSource = "not JSON",
            };

            database.Processor.NormalizeSchemaMetadata(term, "example.csv");

            Assert.Empty(term.SchemaConstraints);
            Assert.Contains(
                logger.Entries,
                entry =>
                    entry.Level == LogLevel.Warning
                    && entry.Message.Contains(
                        "INVALID SCHEMA CONSTRAINT <document>",
                        StringComparison.Ordinal
                    )
                    && entry.Message.Contains("CONSTRAINT EXCLUDED", StringComparison.Ordinal)
            );
        }

        private static void AssertInvalidConstraintLogged(
            CapturingLogger<GlossaryTermProcessor> logger,
            string constraint
        )
        {
            Assert.Contains(
                logger.Entries,
                entry =>
                    entry.Level == LogLevel.Warning
                    && entry.Message.Contains(
                        $"INVALID SCHEMA CONSTRAINT {constraint}",
                        StringComparison.Ordinal
                    )
                    && entry.Message.Contains("example-term", StringComparison.Ordinal)
                    && entry.Message.Contains("example.csv", StringComparison.Ordinal)
                    && entry.Message.Contains("CONSTRAINT EXCLUDED", StringComparison.Ordinal)
            );
        }

        private sealed class CapturingLogger<T> : ILogger<T>
        {
            public List<LogEntry> Entries { get; } = [];

            public IDisposable? BeginScope<TState>(TState state)
                where TState : notnull
            {
                return null;
            }

            public bool IsEnabled(LogLevel logLevel)
            {
                return true;
            }

            public void Log<TState>(
                LogLevel logLevel,
                EventId eventId,
                TState state,
                Exception? exception,
                Func<TState, Exception?, string> formatter
            )
            {
                Entries.Add(new LogEntry(logLevel, formatter(state, exception)));
            }
        }

        private sealed record LogEntry(LogLevel Level, string Message);
    }
}
