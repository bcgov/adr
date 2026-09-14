namespace Adr.Semantics.Data
{
    using System;
    using System.Collections.Generic;

    internal sealed class GlossaryEntity
    {
        public required string Id { get; set; }

        public required string Name { get; set; }

        public long? CurrentReleaseId { get; set; }

        public GlossaryReleaseEntity? CurrentRelease { get; set; }

        public IList<GlossaryReleaseEntity> Releases { get; set; } = [];
    }

    internal sealed class GlossaryReleaseEntity
    {
        public long Id { get; set; }

        public required string GlossaryId { get; set; }

        public GlossaryEntity? Glossary { get; set; }

        public required string Version { get; set; }

        public int Sequence { get; set; }

        public DateOnly PublishedAt { get; set; }

        public required string SourceAsset { get; set; }

        public required string SourceHash { get; set; }

        public DateTime CreatedUtc { get; set; }

        public IList<GlossaryReleaseTermEntity> Terms { get; set; } = [];
    }

    internal sealed class GlossaryTermEntity
    {
        public Guid Id { get; set; }

        public required string GlossaryId { get; set; }

        public required string Name { get; set; }

        public IList<GlossaryTermRevisionEntity> Revisions { get; set; } = [];
    }

    internal sealed class GlossaryTermRevisionEntity
    {
        public long Id { get; set; }

        public Guid TermId { get; set; }

        public GlossaryTermEntity? TermIdentity { get; set; }

        public Guid StaticId { get; set; }

        public int Revision { get; set; }

        public required string Name { get; set; }

        public required string Term { get; set; }

        public required string Definition { get; set; }

        public required string Example { get; set; }

        public required string SchemaType { get; set; }

        public required string SchemaConstraintsJson { get; set; }

        public required string KeywordsJson { get; set; }

        public required string Scope { get; set; }

        public required string ScopeUrl { get; set; }

        public required string Citations { get; set; }

        public required string TeamSource { get; set; }

        public bool VerifiedDefinitionFlag { get; set; }

        public bool PublishToDevHub { get; set; }

        public required string ContentHash { get; set; }

        public IList<GlossaryReleaseTermEntity> Releases { get; set; } = [];
    }

    internal sealed class GlossaryReleaseTermEntity
    {
        public long ReleaseId { get; set; }

        public GlossaryReleaseEntity? Release { get; set; }

        public long TermRevisionId { get; set; }

        public GlossaryTermRevisionEntity? TermRevision { get; set; }
    }

    internal sealed class GlossaryImportEntity
    {
        public long Id { get; set; }

        public required string GlossaryId { get; set; }

        public int Sequence { get; set; }

        public DateOnly PublishedAt { get; set; }

        public required string SourceAsset { get; set; }

        public required string SourceHash { get; set; }

        public required string Status { get; set; }

        public string? Error { get; set; }

        public long? ReleaseId { get; set; }

        public DateTime ImportedUtc { get; set; }

        public IList<GlossaryTermSubmissionEntity> TermSubmissions { get; set; } = [];
    }

    internal sealed class GlossaryTermSubmissionEntity
    {
        public long Id { get; set; }

        public required string GlossaryId { get; set; }

        public long? ImportId { get; set; }

        public GlossaryImportEntity? Import { get; set; }

        public Guid? DraftId { get; set; }

        public GlossaryDraftEntity? Draft { get; set; }

        public int Sequence { get; set; }

        public required string SourceType { get; set; }

        public required string SourceReference { get; set; }

        public required string Operation { get; set; }

        public required string Name { get; set; }

        public required string SubmittedStaticId { get; set; }

        public Guid? ResolvedStaticId { get; set; }

        public required string SourcePayload { get; set; }

        public bool BreakingChange { get; set; }

        public bool IsValid { get; set; }

        public required string InvalidReasonsJson { get; set; }

        public long? TermRevisionId { get; set; }

        public GlossaryTermRevisionEntity? TermRevision { get; set; }

        public DateTime SubmittedUtc { get; set; }
    }

    internal sealed class GlossaryDraftEntity
    {
        public Guid Id { get; set; }

        public required string GlossaryId { get; set; }

        public long BaseReleaseId { get; set; }

        public GlossaryReleaseEntity? BaseRelease { get; set; }

        public required string Status { get; set; }

        public required string SnapshotJson { get; set; }

        public long? PublishedReleaseId { get; set; }

        public GlossaryReleaseEntity? PublishedRelease { get; set; }

        public DateTime CreatedUtc { get; set; }

        public DateTime UpdatedUtc { get; set; }

        public IList<GlossaryTermSubmissionEntity> TermSubmissions { get; set; } = [];
    }
}
