namespace Adr.Semantics.Providers
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using Adr.Semantics.Data;
    using Adr.Semantics.Models;
    using Microsoft.EntityFrameworkCore;

    internal sealed class GlossaryDatabaseProvider : IGlossaryProvider
    {
        private readonly IDbContextFactory<GlossaryDbContext> _contextFactory;

        public GlossaryDatabaseProvider(IDbContextFactory<GlossaryDbContext> contextFactory)
        {
            _contextFactory = contextFactory;
            using var context = _contextFactory.CreateDbContext();
            if (!context.Glossaries.Any(item => item.CurrentReleaseId != null))
            {
                throw new InvalidDataException("No current glossary release is available.");
            }
        }

        /// <inheritdoc/>
        public IEnumerable<GlossaryModel> GetAllGlossaries()
        {
            using var context = _contextFactory.CreateDbContext();
            var releaseId = GetGlossary(context).CurrentReleaseId
                ?? throw new InvalidDataException("No current glossary release is available.");
            return GetTerms(context, releaseId);
        }

        /// <inheritdoc/>
        public IEnumerable<GlossaryModel>? GetAllGlossaries(string version)
        {
            using var context = _contextFactory.CreateDbContext();
            var release = FindRelease(context, version);
            return release is null ? null : GetTerms(context, release.Id);
        }

        /// <inheritdoc/>
        public GlossaryVersionModel GetCurrentVersion()
        {
            using var context = _contextFactory.CreateDbContext();
            var glossary = GetGlossary(context);
            var release = glossary.CurrentReleaseId is null
                ? null
                : context.GlossaryReleases.AsNoTracking().SingleOrDefault(item =>
                    item.Id == glossary.CurrentReleaseId
                );
            return release is null
                ? throw new InvalidDataException("No current glossary release is available.")
                : ToVersionModel(glossary, release);
        }

        /// <inheritdoc/>
        public IEnumerable<GlossaryVersionModel> GetVersions()
        {
            using var context = _contextFactory.CreateDbContext();
            var glossary = GetGlossary(context);
            return context
                .GlossaryReleases.AsNoTracking()
                .Where(item => item.GlossaryId == glossary.Id)
                .OrderByDescending(item => item.Sequence)
                .Select(item =>
                    new GlossaryVersionModel
                    {
                        Id = glossary.Id,
                        Name = glossary.Name,
                        Version = item.Version,
                        PublishedAt = item.PublishedAt,
                        IsCurrent = item.Id == glossary.CurrentReleaseId,
                    }
                )
                .ToList();
        }

        /// <inheritdoc/>
        public GlossaryVersionModel? GetVersion(string version)
        {
            using var context = _contextFactory.CreateDbContext();
            var glossary = GetGlossary(context);
            var release = FindRelease(context, version);
            return release is null ? null : ToVersionModel(glossary, release);
        }

        /// <inheritdoc/>
        public IEnumerable<GlossaryModel>? GetTermVersions(string term)
        {
            using var context = _contextFactory.CreateDbContext();
            var termId = FindKnownTermId(context, term);
            if (termId is null)
            {
                return null;
            }

            return context
                .GlossaryTermRevisions.AsNoTracking()
                .Where(item => item.TermId == termId)
                .OrderByDescending(item => item.Revision)
                .AsEnumerable()
                .Select(GlossaryTermPersistence.ToModel)
                .ToList();
        }

        /// <inheritdoc/>
        public GlossaryModel? GetTermVersion(string term, int termVersion)
        {
            using var context = _contextFactory.CreateDbContext();
            var termId = FindKnownTermId(context, term);
            if (termId is null)
            {
                return null;
            }

            var revision = context
                .GlossaryTermRevisions.AsNoTracking()
                .SingleOrDefault(item => item.TermId == termId && item.Revision == termVersion);
            return revision is null ? null : GlossaryTermPersistence.ToModel(revision);
        }

        /// <inheritdoc/>
        public GlossaryModel? GetGlossaryEntryById(Guid id)
        {
            using var context = _contextFactory.CreateDbContext();
            var releaseId = GetGlossary(context).CurrentReleaseId;
            if (releaseId is null)
            {
                return null;
            }

            var revision = context
                .GlossaryReleaseTerms.AsNoTracking()
                .Where(item => item.ReleaseId == releaseId)
                .Select(item => item.TermRevision!)
                .SingleOrDefault(item => item.StaticId == id);
            return revision is null ? null : GlossaryTermPersistence.ToModel(revision);
        }

        /// <inheritdoc/>
        public IEnumerable<GlossaryTermHistoryModel>? GetTermHistory(
            string glossaryVersion,
            string term
        )
        {
            using var context = _contextFactory.CreateDbContext();
            var release = FindRelease(context, glossaryVersion);
            if (release is null)
            {
                return null;
            }

            var selectedRevision = context
                .GlossaryReleaseTerms.AsNoTracking()
                .Where(item => item.ReleaseId == release.Id)
                .Select(item => item.TermRevision!)
                .AsEnumerable()
                .SingleOrDefault(item =>
                    string.Equals(item.Name, term, StringComparison.OrdinalIgnoreCase)
                );
            if (selectedRevision is null)
            {
                return null;
            }

            var id = selectedRevision.TermId;

            var revisions = context
                .GlossaryTermRevisions.AsNoTracking()
                .Where(item => item.TermId == id)
                .OrderByDescending(item => item.Revision)
                .ToList();
            var staticIds = revisions.Select(item => item.StaticId).ToHashSet();
            var revisionIds = revisions.Select(item => item.Id).ToHashSet();
            var revisionSubmissions = context
                .GlossaryTermSubmissions.AsNoTracking()
                .Where(item =>
                    item.TermRevisionId != null && revisionIds.Contains(item.TermRevisionId.Value)
                )
                .AsEnumerable()
                .GroupBy(item => item.TermRevisionId!.Value)
                .ToDictionary(group =>
                    group.Key,
                    group => group.OrderBy(item => item.SubmittedUtc).ThenBy(item => item.Id).First()
                );
            var releaseVersions = context
                .GlossaryReleaseTerms.AsNoTracking()
                .Where(item => revisionIds.Contains(item.TermRevisionId))
                .Select(item => new
                {
                    item.TermRevisionId,
                    item.Release!.Version,
                    item.Release.Sequence,
                })
                .AsEnumerable()
                .GroupBy(item => item.TermRevisionId)
                .ToDictionary(
                    group => group.Key,
                    group =>
                        (IReadOnlyList<string>)group
                            .OrderByDescending(item => item.Sequence)
                            .Select(item => item.Version)
                            .ToList()
                );

            var history = revisions
                .Select(revision =>
                {
                    revisionSubmissions.TryGetValue(revision.Id, out var submission);
                    return new GlossaryTermHistoryModel
                    {
                        Status = "Published",
                        Version = revision.Revision,
                        Term = GlossaryTermPersistence.ToModel(revision),
                        Name = revision.Name,
                        SubmittedStaticId = revision.StaticId.ToString(),
                        SourceType = submission?.SourceType ?? "Publication",
                        SourceReference = submission?.SourceReference ?? "",
                        Operation = submission?.Operation ?? "Upsert",
                        BreakingChange = submission?.BreakingChange ?? false,
                        GlossaryVersions = releaseVersions.GetValueOrDefault(revision.Id) ?? [],
                        RecordedUtc = submission?.SubmittedUtc ?? DateTime.MinValue,
                    };
                })
                .ToList();

            history.AddRange(
                context
                    .GlossaryTermSubmissions.AsNoTracking()
                    .Where(item =>
                        item.ResolvedStaticId != null
                        && staticIds.Contains(item.ResolvedStaticId.Value)
                        && !item.IsValid
                    )
                    .AsEnumerable()
                    .Select(item => new GlossaryTermHistoryModel
                    {
                        Status = "Invalid",
                        Name = item.Name,
                        SubmittedStaticId = item.SubmittedStaticId,
                        SourceType = item.SourceType,
                        SourceReference = item.SourceReference,
                        Operation = item.Operation,
                        BreakingChange = item.BreakingChange,
                        InvalidReasons = JsonSerializer.Deserialize<List<string>>(
                                item.InvalidReasonsJson
                            ) ?? [],
                        SourcePayload = item.SourcePayload,
                        RecordedUtc = item.SubmittedUtc,
                    })
            );

            return history
                .OrderByDescending(item => item.RecordedUtc)
                .ThenByDescending(item => item.Version ?? int.MaxValue)
                .ToList();
        }

        private static GlossaryEntity GetGlossary(GlossaryDbContext context)
        {
            return context.Glossaries.AsNoTracking().Single();
        }

        private static GlossaryReleaseEntity? FindRelease(
            GlossaryDbContext context,
            string version
        )
        {
            return context.GlossaryReleases.AsNoTracking().SingleOrDefault(item =>
                item.Version == version
            );
        }

        private static List<GlossaryModel> GetTerms(GlossaryDbContext context, long releaseId)
        {
            return context
                .GlossaryReleaseTerms.AsNoTracking()
                .Where(item => item.ReleaseId == releaseId)
                .Select(item => item.TermRevision!)
                .OrderBy(item => item.Name)
                .AsEnumerable()
                .Select(GlossaryTermPersistence.ToModel)
                .ToList();
        }

        private static Guid? FindCurrentTermId(GlossaryDbContext context, string name)
        {
            var glossary = GetGlossary(context);
            if (glossary.CurrentReleaseId is null)
            {
                return null;
            }

            return context
                .GlossaryReleaseTerms.AsNoTracking()
                .Where(item => item.ReleaseId == glossary.CurrentReleaseId)
                .Select(item => item.TermRevision!)
                .AsEnumerable()
                .Where(item => string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                .Select(item => (Guid?)item.TermId)
                .SingleOrDefault();
        }

        private static Guid? FindKnownTermId(GlossaryDbContext context, string name)
        {
            return FindCurrentTermId(context, name)
                ?? context
                    .GlossaryTermRevisions.AsNoTracking()
                    .Select(item => new { item.TermId, item.Name, item.Revision })
                    .AsEnumerable()
                    .Where(item =>
                        string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)
                    )
                    .OrderByDescending(item => item.Revision)
                    .Select(item => (Guid?)item.TermId)
                    .FirstOrDefault();
        }

        private static GlossaryVersionModel ToVersionModel(
            GlossaryEntity glossary,
            GlossaryReleaseEntity release
        )
        {
            return new GlossaryVersionModel
            {
                Id = glossary.Id,
                Name = glossary.Name,
                Version = release.Version,
                PublishedAt = release.PublishedAt,
                IsCurrent = release.Id == glossary.CurrentReleaseId,
            };
        }
    }
}
