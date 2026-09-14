namespace Adr.Semantics.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Adr.Semantics.Data;
    using Adr.Semantics.Models;
    using Microsoft.EntityFrameworkCore;

    internal static class GlossaryTermQueries
    {
        public static List<GlossaryModel> LoadCurrentTerms(
            GlossaryDbContext context,
            GlossaryEntity glossary
        )
        {
            return glossary.CurrentReleaseId is null
                ? []
                : LoadReleaseTerms(context, glossary.CurrentReleaseId.Value);
        }

        public static List<GlossaryModel> LoadReleaseTerms(
            GlossaryDbContext context,
            long releaseId
        )
        {
            return context
                .GlossaryReleaseTerms.AsNoTracking()
                .Where(item => item.ReleaseId == releaseId)
                .Select(item => item.TermRevision!)
                .AsEnumerable()
                .Select(GlossaryTermPersistence.ToModel)
                .ToList();
        }

        public static List<GlossaryModel> LoadKnownTerms(
            GlossaryDbContext context,
            string glossaryId
        )
        {
            return context
                .GlossaryTermRevisions.AsNoTracking()
                .Where(item => item.TermIdentity!.GlossaryId == glossaryId)
                .OrderByDescending(item => item.Id)
                .AsEnumerable()
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => GlossaryTermPersistence.ToModel(group.First()))
                .ToList();
        }

        public static GlossaryTermVersionIndex LoadLatestVersions(
            GlossaryDbContext context,
            string glossaryId
        )
        {
            var revisions = context
                .GlossaryTermRevisions.AsNoTracking()
                .Where(item => item.TermIdentity!.GlossaryId == glossaryId)
                .ToList();
            return new GlossaryTermVersionIndex(revisions);
        }
    }

    internal sealed class GlossaryTermVersionIndex
    {
        private readonly Dictionary<string, int> _versionsByName = new(
            StringComparer.OrdinalIgnoreCase
        );

        public GlossaryTermVersionIndex(IEnumerable<GlossaryTermRevisionEntity> revisions)
        {
            foreach (var identity in revisions.GroupBy(item => item.TermId))
            {
                var latestVersion = identity.Max(item => item.Revision);
                foreach (var revision in identity)
                {
                    SetMaximum(_versionsByName, revision.Name, latestVersion);
                }
            }
        }

        public int GetLatestVersion(GlossaryModel term)
        {
            if (_versionsByName.TryGetValue(term.Name, out var version))
            {
                return version;
            }

            return 0;
        }

        private static void SetMaximum(
            Dictionary<string, int> versions,
            string key,
            int version
        )
        {
            versions.TryGetValue(key, out var currentVersion);
            versions[key] = Math.Max(currentVersion, version);
        }
    }
}
