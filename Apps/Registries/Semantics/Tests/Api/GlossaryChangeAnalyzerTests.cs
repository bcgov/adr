namespace Adr.Semantics.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using Adr.Semantics.Models;
    using Adr.Semantics.Providers;

    public class GlossaryChangeAnalyzerTests
    {
        private static readonly Guid ExampleId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        [Fact]
        public void IdenticalTermsDoNotCreateARelease()
        {
            var before = CreateTerm();
            var after = CreateTerm();

            Assert.Equal(
                GlossaryChangeKind.None,
                GlossaryChangeAnalyzer.Analyze([before], [after])
            );
        }

        [Fact]
        public void DefinitionCorrectionIncrementsPatch()
        {
            var before = CreateTerm();
            var after = CreateTerm(definition: "Corrected definition");

            Assert.Equal(
                GlossaryChangeKind.Patch,
                GlossaryChangeAnalyzer.Analyze([before], [after])
            );
        }

        [Fact]
        public void ExplicitBreakingChangeIncrementsMajor()
        {
            var before = CreateTerm();
            var after = CreateTerm(definition: "Semantically incompatible definition");

            Assert.Equal(
                GlossaryChangeKind.Major,
                GlossaryChangeAnalyzer.Analyze([before], [after], new HashSet<Guid> { ExampleId })
            );
        }

        [Fact]
        public void ExplicitBreakingFlagDoesNotVersionUnchangedContent()
        {
            Assert.Equal(
                GlossaryChangeKind.None,
                GlossaryChangeAnalyzer.Analyze(
                    [CreateTerm()],
                    [CreateTerm()],
                    new HashSet<Guid> { ExampleId }
                )
            );
        }

        [Fact]
        public void AddedTermIncrementsMinor()
        {
            var existing = CreateTerm();
            var added = CreateTerm(
                id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
                name: "added-term"
            );

            Assert.Equal(
                GlossaryChangeKind.Minor,
                GlossaryChangeAnalyzer.Analyze([existing], [existing, added])
            );
        }

        [Fact]
        public void RemovedTermIncrementsMajor()
        {
            Assert.Equal(
                GlossaryChangeKind.Major,
                GlossaryChangeAnalyzer.Analyze([CreateTerm()], [])
            );
        }

        [Fact]
        public void ChangedGuidForExistingSlugIncrementsMajor()
        {
            var before = CreateTerm();
            var after = CreateTerm(id: Guid.Parse("33333333-3333-3333-3333-333333333333"));

            Assert.Equal(
                GlossaryChangeKind.Major,
                GlossaryChangeAnalyzer.Analyze([before], [after])
            );
        }

        [Fact]
        public void ChangedSlugForExistingGuidIncrementsMajor()
        {
            Assert.Equal(
                GlossaryChangeKind.Major,
                GlossaryChangeAnalyzer.Analyze(
                    [CreateTerm()],
                    [CreateTerm(name: "renamed-term")]
                )
            );
        }

        [Theory]
        [InlineData(false, true)]
        [InlineData(true, false)]
        public void RemovingTermFromPublishedOutputsIncrementsMajor(
            bool verified,
            bool published
        )
        {
            Assert.Equal(
                GlossaryChangeKind.Major,
                GlossaryChangeAnalyzer.Analyze(
                    [CreateTerm()],
                    [CreateTerm(verified: verified, published: published)]
                )
            );
        }

        [Fact]
        public void MoreRestrictiveTypeIncrementsMajor()
        {
            Assert.Equal(
                GlossaryChangeKind.Major,
                GlossaryChangeAnalyzer.Analyze(
                    [CreateTerm(schemaType: "number")],
                    [CreateTerm(schemaType: "integer")]
                )
            );
        }

        [Fact]
        public void WidenedNumericTypeIncrementsPatch()
        {
            Assert.Equal(
                GlossaryChangeKind.Patch,
                GlossaryChangeAnalyzer.Analyze(
                    [CreateTerm(schemaType: "integer")],
                    [CreateTerm(schemaType: "number")]
                )
            );
        }

        [Fact]
        public void WidenedNumericTypeWithNewConstraintIncrementsMajor()
        {
            Assert.Equal(
                GlossaryChangeKind.Major,
                GlossaryChangeAnalyzer.Analyze(
                    [CreateTerm(schemaType: "integer")],
                    [CreateTerm(schemaType: "number", minimum: 10)]
                )
            );
        }

        [Fact]
        public void TightenedConstraintIncrementsMajor()
        {
            Assert.Equal(
                GlossaryChangeKind.Major,
                GlossaryChangeAnalyzer.Analyze(
                    [CreateTerm(schemaType: "number", maximum: 100)],
                    [CreateTerm(schemaType: "number", maximum: 99)]
                )
            );
        }

        private static GlossaryModel CreateTerm(
            Guid? id = null,
            string name = "example-term",
            string definition = "Definition",
            string schemaType = "string",
            decimal? minimum = null,
            decimal? maximum = null,
            bool verified = true,
            bool published = true
        )
        {
            var constraints = new Dictionary<string, object>();
            if (minimum is not null)
            {
                constraints["minimum"] = JsonSerializer.SerializeToElement(minimum.Value);
            }

            if (maximum is not null)
            {
                constraints["maximum"] = JsonSerializer.SerializeToElement(maximum.Value);
            }

            return new GlossaryModel
            {
                StaticId = (id ?? ExampleId).ToString(),
                Name = name,
                Term = "Example Term",
                Definition = definition,
                SchemaType = schemaType,
                SchemaConstraints = constraints,
                VerifiedDefinitionFlag = verified,
                PublishToDevHub = published,
            };
        }
    }
}
