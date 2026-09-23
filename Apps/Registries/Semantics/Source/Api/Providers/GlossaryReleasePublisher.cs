namespace Adr.Semantics.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Adr.Semantics.Data;
    using Adr.Semantics.Models;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    internal sealed class GlossaryReleasePublisher
    {
        private readonly ILogger<GlossaryReleasePublisher> _logger;

        public GlossaryReleasePublisher(ILogger<GlossaryReleasePublisher> logger)
        {
            _logger = logger;
        }

        public GlossaryPublicationResult? Publish(
            GlossaryDbContext context,
            GlossaryEntity glossary,
            DateOnly publishedAt,
            string sourceReference,
            string sourceHash,
            IReadOnlyList<GlossaryModel> publishedTerms,
            IReadOnlySet<Guid> explicitlyBreakingTermIds,
            bool forceReleaseBoundary,
            Guid? publishingDraftId = null
        )
        {
            var currentRelease = glossary.CurrentReleaseId is null
                ? null
                : context
                    .GlossaryReleases.Include(item => item.Terms)
                    .ThenInclude(item => item.TermRevision)
                    .Single(item => item.Id == glossary.CurrentReleaseId);
            var currentTerms = currentRelease is null
                ? []
                : currentRelease.Terms.Select(item =>
                    GlossaryTermPersistence.ToModel(item.TermRevision!)
                ).ToList();
            var change = currentRelease is null
                ? GlossaryChangeKind.Minor
                : GlossaryChangeAnalyzer.Analyze(
                    currentTerms,
                    publishedTerms,
                    explicitlyBreakingTermIds
                );

            if (change == GlossaryChangeKind.None)
            {
                if (!forceReleaseBoundary)
                {
                    return null;
                }

                change = GlossaryChangeKind.Patch;
            }

            var version = currentRelease is null
                ? GlossarySemanticVersion.Initial
                : GlossarySemanticVersion.Parse(currentRelease.Version).Increment(change);
            var release = new GlossaryReleaseEntity
            {
                GlossaryId = glossary.Id,
                Version = version.ToString(),
                Sequence = (currentRelease?.Sequence ?? 0) + 1,
                PublishedAt = publishedAt,
                SourceAsset = sourceReference,
                SourceHash = sourceHash,
                CreatedUtc = DateTime.UtcNow,
            };
            context.GlossaryReleases.Add(release);

            var identities = context
                .GlossaryTerms.Include(item => item.Revisions)
                .Where(item => item.GlossaryId == glossary.Id)
                .ToDictionary(item => item.Name, StringComparer.OrdinalIgnoreCase);
            var currentRevisions = currentRelease?.Terms.Select(item => item.TermRevision!).ToList()
                ?? [];
            var releaseRevisions = new Dictionary<Guid, GlossaryTermRevisionEntity>();

            foreach (var term in publishedTerms.OrderBy(item => item.Name, StringComparer.Ordinal))
            {
                var staticId = Guid.Parse(term.StaticId);
                if (!identities.TryGetValue(term.Name, out var identity))
                {
                    identity = new GlossaryTermEntity
                    {
                        Id = Guid.NewGuid(),
                        GlossaryId = glossary.Id,
                        Name = term.Name,
                    };
                    context.GlossaryTerms.Add(identity);
                    identities.Add(identity.Name, identity);
                }

                var currentRevision = currentRevisions.SingleOrDefault(item =>
                    item.TermId == identity.Id
                );

                var revision =
                    currentRevision is null
                    || currentRevision.StaticId != staticId
                    || !string.Equals(
                        currentRevision.ContentHash,
                        GlossaryTermPersistence.GetContentHash(term),
                        StringComparison.Ordinal
                    )
                        ? CreateRevision(identity, term)
                        : currentRevision;
                releaseRevisions[staticId] = revision;
                release.Terms.Add(new GlossaryReleaseTermEntity { TermRevision = revision });
            }

            context.SaveChanges();
            glossary.CurrentReleaseId = release.Id;
            if (currentRelease is not null)
            {
                var now = DateTime.UtcNow;
                var staleDrafts = context.GlossaryDrafts.Where(item =>
                    item.GlossaryId == glossary.Id
                    && item.BaseReleaseId == currentRelease.Id
                    && item.Status == "Draft"
                    && (publishingDraftId == null || item.Id != publishingDraftId)
                );
                foreach (var draft in staleDrafts)
                {
                    draft.Status = "Stale";
                    draft.UpdatedUtc = now;
                }
            }

            context.SaveChanges();

            _logger.LogInformation(
                "Published glossary version {GlossaryVersion} from {SourceReference} ({ChangeKind}).",
                release.Version,
                sourceReference,
                change
            );

            return new GlossaryPublicationResult(release, change, releaseRevisions);
        }

        private static GlossaryTermRevisionEntity CreateRevision(
            GlossaryTermEntity identity,
            GlossaryModel term
        )
        {
            var revision = GlossaryTermPersistence.ToEntity(
                term,
                identity.Revisions.Select(item => item.Revision).DefaultIfEmpty().Max() + 1
            );
            revision.TermIdentity = identity;
            identity.Revisions.Add(revision);
            return revision;
        }
    }

    internal sealed record GlossaryPublicationResult(
        GlossaryReleaseEntity Release,
        GlossaryChangeKind Change,
        IReadOnlyDictionary<Guid, GlossaryTermRevisionEntity> ReleaseRevisions
    );
}
