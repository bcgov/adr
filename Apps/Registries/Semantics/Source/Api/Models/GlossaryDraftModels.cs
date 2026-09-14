namespace Adr.Semantics.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Describes an unpublished glossary draft.
    /// </summary>
    public sealed class GlossaryDraftModel
    {
        /// <summary>Gets or sets the draft identifier.</summary>
        public Guid Id { get; set; }

        /// <summary>Gets or sets the glossary version on which the draft is based.</summary>
        public required string BaseVersion { get; set; }

        /// <summary>Gets or sets the draft status.</summary>
        public required string Status { get; set; }

        /// <summary>Gets or sets the calculated draft or published version.</summary>
        public required string Version { get; set; }

        /// <summary>Gets or sets the aggregate semantic change.</summary>
        public required string ChangeType { get; set; }

        /// <summary>Gets or sets when the draft was created.</summary>
        public DateTime CreatedUtc { get; set; }

        /// <summary>Gets or sets when the draft was last updated.</summary>
        public DateTime UpdatedUtc { get; set; }
    }

    /// <summary>
    /// Describes the effective content and version impact of a glossary draft.
    /// </summary>
    public sealed class GlossaryDraftPreviewModel
    {
        /// <summary>Gets or sets the draft metadata.</summary>
        public required GlossaryDraftModel Draft { get; set; }

        /// <summary>Gets or sets the effective published terms.</summary>
        public required IEnumerable<GlossaryModel> Terms { get; set; }

        /// <summary>Gets or sets terms that differ from the draft's base release.</summary>
        public required IEnumerable<GlossaryDraftTermChangeModel> TermChanges { get; set; }
    }

    /// <summary>
    /// Describes a term added, changed, or deleted in a glossary draft.
    /// </summary>
    public sealed class GlossaryDraftTermChangeModel
    {
        /// <summary>Gets or sets the term using its projected published version.</summary>
        public required GlossaryModel Term { get; set; }

        /// <summary>
        /// Gets or sets the term as it appeared in the draft's base glossary release.
        /// </summary>
        public GlossaryModel? OriginalTerm { get; set; }

        /// <summary>Gets or sets New, Bugfix, Breaking Change, or Deleted.</summary>
        public required string ChangeType { get; set; }

        /// <summary>
        /// Gets or sets whether the latest saved term submission declares a breaking change.
        /// </summary>
        public bool BreakingChange { get; set; }
    }

    /// <summary>
    /// Represents an editable glossary term without server-managed version metadata.
    /// </summary>
    public sealed class GlossaryTermEditModel
    {
        /// <summary>Gets or sets the optional stable term UUID.</summary>
        [JsonPropertyName("id")]
        public string StaticId { get; set; } = "";

        /// <summary>Gets or sets the displayed term.</summary>
        public required string Term { get; set; }

        /// <summary>Gets or sets the published definition.</summary>
        public string Definition { get; set; } = "";

        /// <summary>Gets or sets an example value.</summary>
        public string Example { get; set; } = "";

        /// <summary>Gets or sets the OpenAPI schema type.</summary>
        public string SchemaType { get; set; } = "string";

        /// <summary>Gets or sets the serialized OpenAPI constraints.</summary>
        public string SchemaConstraints { get; set; } = "";

        /// <summary>Gets or sets the search keywords.</summary>
        public IList<string> Keywords { get; set; } = [];

        /// <summary>Gets or sets the scope.</summary>
        public string Scope { get; set; } = "";

        /// <summary>Gets or sets the scope URL.</summary>
        public string ScopeUrl { get; set; } = "";

        /// <summary>Gets or sets the citations.</summary>
        public string Citations { get; set; } = "";

        /// <summary>Gets or sets the team source.</summary>
        public string TeamSource { get; set; } = "";

        /// <summary>Gets or sets whether the definition is verified.</summary>
        public bool VerifiedDefinitionFlag { get; set; }

        /// <summary>Gets or sets whether the term is published.</summary>
        public bool PublishToDevHub { get; set; }

        /// <summary>
        /// Gets or sets whether this update must be treated as a breaking change.
        /// </summary>
        public bool BreakingChange { get; set; }
    }

    /// <summary>
    /// Reports whether a draft term submission was accepted.
    /// </summary>
    public sealed class GlossaryTermSubmissionResultModel
    {
        /// <summary>Gets or sets whether the submission was valid.</summary>
        public bool IsValid { get; set; }

        /// <summary>Gets or sets the validation reasons.</summary>
        public required IReadOnlyList<string> InvalidReasons { get; set; }

        /// <summary>Gets or sets the normalized term when it could be read.</summary>
        public GlossaryModel? Term { get; set; }
    }

    /// <summary>
    /// Reports the result of publishing a glossary draft.
    /// </summary>
    public sealed class GlossaryDraftPublishResultModel
    {
        /// <summary>Gets or sets the publication result status.</summary>
        public required string Status { get; set; }

        /// <summary>Gets or sets the published release when successful.</summary>
        public GlossaryVersionModel? Release { get; set; }

        /// <summary>Gets or sets unresolved invalid term submissions.</summary>
        public IReadOnlyList<GlossaryInvalidTermModel> InvalidTerms { get; set; } = [];
    }

    /// <summary>
    /// Reports the result of applying a stale draft's changes to the current glossary.
    /// </summary>
    public sealed class GlossaryDraftRebaseResultModel
    {
        /// <summary>Gets or sets Rebased or Conflict.</summary>
        public required string Status { get; set; }

        /// <summary>Gets or sets the recovered draft preview when rebasing succeeds.</summary>
        public GlossaryDraftPreviewModel? Preview { get; set; }

        /// <summary>Gets or sets terms changed differently in both releases.</summary>
        public IReadOnlyList<string> ConflictingTerms { get; set; } = [];
    }

    /// <summary>
    /// Identifies a glossary draft to publish as a version.
    /// </summary>
    public sealed class GlossaryVersionPublishModel
    {
        /// <summary>Gets or sets the draft identifier.</summary>
        public required Guid DraftId { get; set; }

        /// <summary>
        /// Gets or sets whether unresolved invalid term submissions may be ignored.
        /// </summary>
        public bool IgnoreInvalid { get; set; }
    }

    /// <summary>
    /// Describes an invalid term submission that prevents draft publication.
    /// </summary>
    public sealed class GlossaryInvalidTermModel
    {
        /// <summary>Gets or sets the submitted term slug.</summary>
        public required string Name { get; set; }

        /// <summary>Gets or sets the validation reasons.</summary>
        public required IReadOnlyList<string> InvalidReasons { get; set; }
    }
}
