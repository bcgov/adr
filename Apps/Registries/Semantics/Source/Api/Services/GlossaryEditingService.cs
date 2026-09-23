namespace Adr.Semantics.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using Adr.Semantics.Data;
    using Adr.Semantics.Models;
    using Adr.Semantics.Providers;
    using Microsoft.EntityFrameworkCore;

    internal sealed class GlossaryEditingService : IGlossaryEditingService
    {
        private const string DraftStatus = "Draft";
        private const string PublishedStatus = "Published";
        private const string StaleStatus = "Stale";

        private static readonly JsonSerializerOptions _serializerOptions = new(
            JsonSerializerDefaults.Web
        );

        private readonly IDbContextFactory<GlossaryDbContext> _contextFactory;
        private readonly GlossaryReleasePublisher _releasePublisher;
        private readonly GlossaryTermProcessor _termProcessor;

        public GlossaryEditingService(
            IDbContextFactory<GlossaryDbContext> contextFactory,
            GlossaryReleasePublisher releasePublisher,
            GlossaryTermProcessor termProcessor
        )
        {
            _contextFactory = contextFactory;
            _releasePublisher = releasePublisher;
            _termProcessor = termProcessor;
        }

        public GlossaryDraftPreviewModel CreateDraft()
        {
            using var context = _contextFactory.CreateDbContext();
            var glossary = context.Glossaries.Single();
            var baseRelease = context.GlossaryReleases.Single(item =>
                item.Id == glossary.CurrentReleaseId
            );
            var terms = GlossaryTermQueries.LoadReleaseTerms(context, baseRelease.Id);
            var now = DateTime.UtcNow;
            var draft = new GlossaryDraftEntity
            {
                Id = Guid.NewGuid(),
                GlossaryId = glossary.Id,
                BaseReleaseId = baseRelease.Id,
                Status = DraftStatus,
                SnapshotJson = SerializeTerms(terms),
                CreatedUtc = now,
                UpdatedUtc = now,
            };
            context.GlossaryDrafts.Add(draft);
            context.SaveChanges();
            return ToPreview(
                draft,
                baseRelease,
                terms,
                terms,
                null,
                new HashSet<Guid>(),
                GlossaryTermQueries.LoadLatestVersions(context, glossary.Id)
            );
        }

        public IList<GlossaryDraftModel> GetDrafts(string? status = null)
        {
            using var context = _contextFactory.CreateDbContext();
            var drafts = context
                .GlossaryDrafts.AsNoTracking()
                .OrderByDescending(item => item.CreatedUtc)
                .ThenByDescending(item => item.Id)
                .ToList();
            var glossary = context.Glossaries.AsNoTracking().Single();
            var currentReleaseId = glossary.CurrentReleaseId;
            var latestTermVersions = GlossaryTermQueries.LoadLatestVersions(
                context,
                glossary.Id
            );
            foreach (var draft in drafts)
            {
                SetEffectiveStatus(draft, currentReleaseId);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                drafts = drafts
                    .Where(item =>
                        string.Equals(item.Status, status.Trim(), StringComparison.OrdinalIgnoreCase)
                    )
                    .ToList();
            }

            return drafts
                .Select(draft =>
                {
                    var baseRelease = context.GlossaryReleases.AsNoTracking().Single(item =>
                        item.Id == draft.BaseReleaseId
                    );
                    var publishedRelease = draft.PublishedReleaseId is null
                        ? null
                        : context.GlossaryReleases.AsNoTracking().Single(item =>
                            item.Id == draft.PublishedReleaseId
                        );
                    var baseTerms = GlossaryTermQueries.LoadReleaseTerms(
                        context,
                        baseRelease.Id
                    );
                    var terms = publishedRelease is null
                        ? DeserializeTerms(draft.SnapshotJson)
                        : GlossaryTermQueries.LoadReleaseTerms(context, publishedRelease.Id);
                    return ToPreview(
                        draft,
                        baseRelease,
                        baseTerms,
                        terms,
                        publishedRelease,
                        GetExplicitlyBreakingTermIds(context, draft.Id),
                        latestTermVersions
                    ).Draft;
                })
                .ToList();
        }

        public GlossaryDraftPreviewModel? GetDraft(Guid draftId)
        {
            using var context = _contextFactory.CreateDbContext();
            var draft = context.GlossaryDrafts.AsNoTracking().SingleOrDefault(item =>
                item.Id == draftId
            );
            if (draft is null)
            {
                return null;
            }

            var currentReleaseId = context
                .Glossaries.AsNoTracking()
                .Single(item => item.Id == draft.GlossaryId)
                .CurrentReleaseId;
            SetEffectiveStatus(draft, currentReleaseId);

            var baseRelease = context.GlossaryReleases.AsNoTracking().Single(item =>
                item.Id == draft.BaseReleaseId
            );
            var publishedRelease = draft.PublishedReleaseId is null
                ? null
                : context.GlossaryReleases.AsNoTracking().Single(item =>
                    item.Id == draft.PublishedReleaseId
                );
            var baseTerms = GlossaryTermQueries.LoadReleaseTerms(context, baseRelease.Id);
            var terms = publishedRelease is null
                ? DeserializeTerms(draft.SnapshotJson)
                : GlossaryTermQueries.LoadReleaseTerms(context, publishedRelease.Id);
            return ToPreview(
                draft,
                baseRelease,
                baseTerms,
                terms,
                publishedRelease,
                GetExplicitlyBreakingTermIds(context, draft.Id),
                GlossaryTermQueries.LoadLatestVersions(context, draft.GlossaryId)
            );
        }

        public GlossaryTermSubmissionResultModel? PutTerm(
            Guid draftId,
            string name,
            GlossaryTermEditModel edit
        )
        {
            using var context = _contextFactory.CreateDbContext();
            var draft = context.GlossaryDrafts.SingleOrDefault(item => item.Id == draftId);
            if (draft is null)
            {
                return null;
            }

            var currentReleaseId = context.Glossaries.Single(item =>
                item.Id == draft.GlossaryId
            ).CurrentReleaseId;
            if (SetEffectiveStatus(draft, currentReleaseId))
            {
                draft.UpdatedUtc = DateTime.UtcNow;
                context.SaveChanges();
            }

            EnsureEditable(draft);
            var currentTerms = DeserializeTerms(draft.SnapshotJson);
            var candidate = ToGlossaryModel(name, edit);
            var submissions = currentTerms
                .Where(item => !string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Name, StringComparer.Ordinal)
                .Select((item, index) =>
                    GlossaryTermSubmission.FromExistingTerm(index + 1, item)
                )
                .ToList();
            var candidateSequence = submissions.Count + 1;
            submissions.Add(
                GlossaryTermSubmission.FromTerm(
                    candidateSequence,
                    candidate,
                    edit.BreakingChange,
                    JsonSerializer.Serialize(edit, _serializerOptions)
                )
            );
            var processing = _termProcessor.Process(
                submissions,
                currentTerms,
                $"draft:{draft.Id}",
                GlossaryTermQueries.LoadKnownTerms(context, draft.GlossaryId)
            );
            var attempt = processing.Attempts.Single(item =>
                item.Sequence == candidateSequence
            );
            var submission = GlossaryTermSubmissionPersistence.ToEntity(
                draft.GlossaryId,
                "API",
                $"draft:{draft.Id}",
                "Upsert",
                attempt
            );
            submission.Sequence = NextSubmissionSequence(context, draft.Id);
            submission.Draft = draft;
            context.GlossaryTermSubmissions.Add(submission);

            if (attempt.IsValid)
            {
                draft.SnapshotJson = SerializeTerms(processing.EffectiveTerms);
                draft.UpdatedUtc = DateTime.UtcNow;
            }

            context.SaveChanges();
            return ToSubmissionResult(attempt);
        }

        public GlossaryTermSubmissionResultModel? DeleteTerm(Guid draftId, string name)
        {
            using var context = _contextFactory.CreateDbContext();
            var draft = context.GlossaryDrafts.SingleOrDefault(item => item.Id == draftId);
            if (draft is null)
            {
                return null;
            }

            var currentReleaseId = context.Glossaries.Single(item =>
                item.Id == draft.GlossaryId
            ).CurrentReleaseId;
            if (SetEffectiveStatus(draft, currentReleaseId))
            {
                draft.UpdatedUtc = DateTime.UtcNow;
                context.SaveChanges();
            }

            EnsureEditable(draft);
            var terms = DeserializeTerms(draft.SnapshotJson);
            var term = terms.SingleOrDefault(item =>
                string.Equals(item.Name, name, StringComparison.OrdinalIgnoreCase)
            );
            if (term is null)
            {
                return null;
            }

            terms.Remove(term);
            var sequence = NextSubmissionSequence(context, draft.Id);
            var attempt = new GlossaryTermProcessingAttempt(
                sequence,
                term.Name,
                term.StaticId,
                Guid.Parse(term.StaticId),
                term,
                JsonSerializer.Serialize(new { term.Name }, _serializerOptions),
                false,
                true,
                []
            );
            var submission = GlossaryTermSubmissionPersistence.ToEntity(
                draft.GlossaryId,
                "API",
                $"draft:{draft.Id}",
                "Delete",
                attempt
            );
            submission.Draft = draft;
            context.GlossaryTermSubmissions.Add(submission);
            draft.SnapshotJson = SerializeTerms(terms);
            draft.UpdatedUtc = DateTime.UtcNow;
            context.SaveChanges();
            return ToSubmissionResult(attempt);
        }

        public GlossaryDraftRebaseResultModel? Rebase(Guid draftId)
        {
            using var context = _contextFactory.CreateDbContext();
            using var transaction = context.Database.BeginTransaction();
            var draft = context.GlossaryDrafts.SingleOrDefault(item => item.Id == draftId);
            if (draft is null)
            {
                return null;
            }

            if (string.Equals(draft.Status, PublishedStatus, StringComparison.Ordinal))
            {
                throw new GlossaryDraftConflictException(
                    $"Glossary draft '{draft.Id}' is already published and cannot be rebased."
                );
            }

            var glossary = context.Glossaries.Single(item => item.Id == draft.GlossaryId);
            var currentRelease = context.GlossaryReleases.Single(item =>
                item.Id == glossary.CurrentReleaseId
            );
            if (draft.BaseReleaseId == currentRelease.Id)
            {
                draft.Status = DraftStatus;
                draft.UpdatedUtc = DateTime.UtcNow;
                context.SaveChanges();
                transaction.Commit();
                var terms = DeserializeTerms(draft.SnapshotJson);
                return new GlossaryDraftRebaseResultModel
                {
                    Status = "Rebased",
                    Preview = ToPreview(
                        draft,
                        currentRelease,
                        GlossaryTermQueries.LoadReleaseTerms(context, currentRelease.Id),
                        terms,
                        null,
                        GetExplicitlyBreakingTermIds(context, draft.Id),
                        GlossaryTermQueries.LoadLatestVersions(context, draft.GlossaryId)
                    ),
                };
            }

            var baseRelease = context.GlossaryReleases.Single(item =>
                item.Id == draft.BaseReleaseId
            );
            var baseTerms = GlossaryTermQueries.LoadReleaseTerms(context, baseRelease.Id);
            var draftTerms = DeserializeTerms(draft.SnapshotJson);
            var currentTerms = GlossaryTermQueries.LoadReleaseTerms(
                context,
                currentRelease.Id
            );
            var merge = MergeTerms(baseTerms, draftTerms, currentTerms);
            if (merge.ConflictingTerms.Count > 0)
            {
                draft.Status = StaleStatus;
                draft.UpdatedUtc = DateTime.UtcNow;
                context.SaveChanges();
                transaction.Commit();
                return new GlossaryDraftRebaseResultModel
                {
                    Status = "Conflict",
                    ConflictingTerms = merge.ConflictingTerms,
                };
            }

            draft.BaseReleaseId = currentRelease.Id;
            draft.SnapshotJson = SerializeTerms(merge.Terms);
            draft.Status = DraftStatus;
            draft.UpdatedUtc = DateTime.UtcNow;
            context.SaveChanges();
            transaction.Commit();

            return new GlossaryDraftRebaseResultModel
            {
                Status = "Rebased",
                Preview = ToPreview(
                    draft,
                    currentRelease,
                    currentTerms,
                    merge.Terms,
                    null,
                    GetExplicitlyBreakingTermIds(context, draft.Id),
                    GlossaryTermQueries.LoadLatestVersions(context, draft.GlossaryId)
                ),
            };
        }

        public GlossaryDraftPublishResultModel? Publish(Guid draftId, bool ignoreInvalid)
        {
            using var context = _contextFactory.CreateDbContext();
            using var transaction = context.Database.BeginTransaction();
            var draft = context.GlossaryDrafts.SingleOrDefault(item => item.Id == draftId);
            if (draft is null)
            {
                return null;
            }

            if (
                string.Equals(draft.Status, PublishedStatus, StringComparison.Ordinal)
                && draft.PublishedReleaseId is not null
            )
            {
                var publishedGlossary = context.Glossaries.Single(item =>
                    item.Id == draft.GlossaryId
                );
                var publishedRelease = context.GlossaryReleases.Single(item =>
                    item.Id == draft.PublishedReleaseId
                );
                return new GlossaryDraftPublishResultModel
                {
                    Status = PublishedStatus,
                    Release = ToVersionModel(publishedGlossary, publishedRelease),
                };
            }

            if (string.Equals(draft.Status, StaleStatus, StringComparison.Ordinal))
            {
                return new GlossaryDraftPublishResultModel { Status = StaleStatus };
            }

            EnsureEditable(draft);
            var glossary = context.Glossaries.Single(item => item.Id == draft.GlossaryId);
            if (glossary.CurrentReleaseId != draft.BaseReleaseId)
            {
                draft.Status = StaleStatus;
                draft.UpdatedUtc = DateTime.UtcNow;
                context.SaveChanges();
                transaction.Commit();
                return new GlossaryDraftPublishResultModel { Status = StaleStatus };
            }

            var baseRelease = context.GlossaryReleases.Single(item =>
                item.Id == draft.BaseReleaseId
            );
            var baseTerms = GlossaryTermQueries.LoadReleaseTerms(context, baseRelease.Id);
            var draftTerms = DeserializeTerms(draft.SnapshotJson);
            var invalidTerms = GetUnresolvedInvalidTerms(context, draft.Id);
            if (invalidTerms.Count > 0 && !ignoreInvalid)
            {
                return new GlossaryDraftPublishResultModel
                {
                    Status = "Invalid",
                    InvalidTerms = invalidTerms,
                };
            }

            var explicitlyBreakingTermIds = GetExplicitlyBreakingTermIds(context, draft.Id);
            var change = GlossaryChangeAnalyzer.Analyze(
                baseTerms,
                draftTerms,
                explicitlyBreakingTermIds
            );
            if (change == GlossaryChangeKind.None)
            {
                return new GlossaryDraftPublishResultModel { Status = "NoChanges" };
            }

            var validation = _termProcessor.Process(
                draftTerms.Select((item, index) =>
                    GlossaryTermSubmission.FromExistingTerm(index + 1, item)
                ),
                baseTerms,
                $"draft:{draft.Id}",
                GlossaryTermQueries.LoadKnownTerms(context, draft.GlossaryId)
            );
            if (validation.Attempts.Any(item => !item.IsValid))
            {
                return new GlossaryDraftPublishResultModel { Status = "Invalid" };
            }

            var sourcePayload = SerializeTerms(draftTerms);
            var sourceHash = Convert.ToHexString(
                SHA256.HashData(Encoding.UTF8.GetBytes(sourcePayload))
            );
            var publication = _releasePublisher.Publish(
                    context,
                    glossary,
                    DateOnly.FromDateTime(DateTime.UtcNow),
                    $"draft:{draft.Id}",
                    sourceHash,
                    draftTerms,
                    explicitlyBreakingTermIds,
                    forceReleaseBoundary: false,
                    publishingDraftId: draft.Id
                )
                ?? throw new InvalidOperationException(
                    "A changed glossary draft did not produce a release."
                );

            LinkPublishedSubmissions(context, draft.Id, publication.ReleaseRevisions);
            draft.Status = PublishedStatus;
            draft.PublishedReleaseId = publication.Release.Id;
            draft.UpdatedUtc = DateTime.UtcNow;
            context.SaveChanges();
            transaction.Commit();

            return new GlossaryDraftPublishResultModel
            {
                Status = PublishedStatus,
                Release = ToVersionModel(glossary, publication.Release),
            };
        }

        private static GlossaryDraftMergeResult MergeTerms(
            IEnumerable<GlossaryModel> baseTerms,
            IEnumerable<GlossaryModel> draftTerms,
            IEnumerable<GlossaryModel> currentTerms
        )
        {
            var baseline = baseTerms.ToDictionary(item => Guid.Parse(item.StaticId));
            var draft = draftTerms.ToDictionary(item => Guid.Parse(item.StaticId));
            var current = currentTerms.ToDictionary(item => Guid.Parse(item.StaticId));
            var merged = current.ToDictionary(item => item.Key, item => item.Value);
            var conflicts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var (termId, baseTerm) in baseline)
            {
                var draftHasTerm = draft.TryGetValue(termId, out var draftTerm);
                var currentHasTerm = current.TryGetValue(termId, out var currentTerm);
                var draftChanged = !draftHasTerm || !TermsEqual(baseTerm, draftTerm!);
                if (!draftChanged)
                {
                    continue;
                }

                var currentChanged = !currentHasTerm || !TermsEqual(baseTerm, currentTerm!);
                if (currentChanged)
                {
                    var sameResult =
                        !draftHasTerm && !currentHasTerm
                        || draftHasTerm
                            && currentHasTerm
                            && TermsEqual(draftTerm!, currentTerm!);
                    if (!sameResult)
                    {
                        conflicts.Add(draftTerm?.Name ?? currentTerm?.Name ?? baseTerm.Name);
                    }

                    continue;
                }

                if (draftHasTerm)
                {
                    merged[termId] = draftTerm!;
                }
                else
                {
                    merged.Remove(termId);
                }
            }

            foreach (var (termId, draftTerm) in draft.Where(item => !baseline.ContainsKey(item.Key)))
            {
                if (merged.TryGetValue(termId, out var currentTerm))
                {
                    if (!TermsEqual(draftTerm, currentTerm))
                    {
                        conflicts.Add(draftTerm.Name);
                    }

                    continue;
                }

                var sameName = merged.Values.FirstOrDefault(item =>
                    string.Equals(item.Name, draftTerm.Name, StringComparison.OrdinalIgnoreCase)
                );
                if (sameName is not null)
                {
                    conflicts.Add(draftTerm.Name);
                    continue;
                }

                merged[termId] = draftTerm;
            }

            return new GlossaryDraftMergeResult(
                merged.Values.OrderBy(item => item.Name, StringComparer.Ordinal).ToList(),
                conflicts.OrderBy(item => item, StringComparer.Ordinal).ToList()
            );
        }

        private static bool TermsEqual(GlossaryModel left, GlossaryModel right)
        {
            return string.Equals(
                GlossaryTermPersistence.GetContentHash(left),
                GlossaryTermPersistence.GetContentHash(right),
                StringComparison.Ordinal
            );
        }

        private sealed record GlossaryDraftMergeResult(
            List<GlossaryModel> Terms,
            IReadOnlyList<string> ConflictingTerms
        );

        private static bool SetEffectiveStatus(
            GlossaryDraftEntity draft,
            long? currentReleaseId
        )
        {
            if (
                !string.Equals(draft.Status, DraftStatus, StringComparison.Ordinal)
                || draft.BaseReleaseId == currentReleaseId
            )
            {
                return false;
            }

            draft.Status = StaleStatus;
            return true;
        }

        private static void EnsureEditable(GlossaryDraftEntity draft)
        {
            if (!string.Equals(draft.Status, DraftStatus, StringComparison.Ordinal))
            {
                throw new GlossaryDraftConflictException(
                    $"Glossary draft '{draft.Id}' is not editable because its status is '{draft.Status}'."
                );
            }
        }

        private static GlossaryModel ToGlossaryModel(
            string name,
            GlossaryTermEditModel edit
        )
        {
            return new GlossaryModel
            {
                StaticId = edit.StaticId,
                Name = name,
                Term = edit.Term,
                Definition = edit.Definition,
                Example = edit.Example,
                SchemaType = edit.SchemaType,
                SchemaConstraintsSource = edit.SchemaConstraints,
                Keywords = edit.Keywords,
                Scope = edit.Scope,
                ScopeUrl = edit.ScopeUrl,
                Citations = edit.Citations,
                TeamSource = edit.TeamSource,
                VerifiedDefinitionFlag = edit.VerifiedDefinitionFlag,
                PublishToDevHub = edit.PublishToDevHub,
                BreakingChange = edit.BreakingChange,
            };
        }

        private static GlossaryTermSubmissionResultModel ToSubmissionResult(
            GlossaryTermProcessingAttempt attempt
        )
        {
            return new GlossaryTermSubmissionResultModel
            {
                IsValid = attempt.IsValid,
                InvalidReasons = attempt.InvalidReasons,
                Term = attempt.Term,
            };
        }

        private static int NextSubmissionSequence(GlossaryDbContext context, Guid draftId)
        {
            return (
                    context
                        .GlossaryTermSubmissions.Where(item => item.DraftId == draftId)
                        .Select(item => (int?)item.Sequence)
                        .Max() ?? 0
                ) + 1;
        }

        private static List<GlossaryInvalidTermModel> GetUnresolvedInvalidTerms(
            GlossaryDbContext context,
            Guid draftId
        )
        {
            return context
                .GlossaryTermSubmissions.AsNoTracking()
                .Where(item => item.DraftId == draftId)
                .AsEnumerable()
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(item => item.Sequence).First())
                .Where(item => !item.IsValid)
                .OrderBy(item => item.Name, StringComparer.Ordinal)
                .Select(item => new GlossaryInvalidTermModel
                {
                    Name = item.Name,
                    InvalidReasons = JsonSerializer.Deserialize<List<string>>(
                            item.InvalidReasonsJson,
                            _serializerOptions
                        ) ?? [],
                })
                .ToList();
        }

        private static HashSet<Guid> GetExplicitlyBreakingTermIds(
            GlossaryDbContext context,
            Guid draftId
        )
        {
            return context
                .GlossaryTermSubmissions.AsNoTracking()
                .Where(item => item.DraftId == draftId && item.IsValid)
                .AsEnumerable()
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.OrderByDescending(item => item.Sequence).First())
                .Where(item =>
                    item.Operation == "Upsert"
                    && item.BreakingChange
                    && item.ResolvedStaticId is not null
                )
                .Select(item => item.ResolvedStaticId!.Value)
                .ToHashSet();
        }

        private static void LinkPublishedSubmissions(
            GlossaryDbContext context,
            Guid draftId,
            IReadOnlyDictionary<Guid, GlossaryTermRevisionEntity> revisions
        )
        {
            var submissions = context
                .GlossaryTermSubmissions.Where(item =>
                    item.DraftId == draftId
                    && item.IsValid
                    && item.Operation == "Upsert"
                    && item.ResolvedStaticId != null
                )
                .OrderByDescending(item => item.Sequence)
                .ToList();
            foreach (var group in submissions.GroupBy(item => item.ResolvedStaticId!.Value))
            {
                if (revisions.TryGetValue(group.Key, out var revision))
                {
                    group.First().TermRevision = revision;
                }
            }
        }

        private static GlossaryDraftPreviewModel ToPreview(
            GlossaryDraftEntity draft,
            GlossaryReleaseEntity baseRelease,
            List<GlossaryModel> baseTerms,
            List<GlossaryModel> terms,
            GlossaryReleaseEntity? publishedRelease,
            IReadOnlySet<Guid> explicitlyBreakingTermIds,
            GlossaryTermVersionIndex latestTermVersions
        )
        {
            var baseVersion = GlossarySemanticVersion.Parse(baseRelease.Version);
            var change = publishedRelease is null
                ? GlossaryChangeAnalyzer.Analyze(
                    baseTerms,
                    terms,
                    explicitlyBreakingTermIds
                )
                : GlossaryChangeKind.None;
            var nextVersion = baseVersion.Increment(
                change == GlossaryChangeKind.None ? GlossaryChangeKind.Patch : change
            );
            var termChanges = DescribeTermChanges(
                baseTerms,
                terms,
                explicitlyBreakingTermIds,
                latestTermVersions,
                projectVersions: publishedRelease is null
            );
            return new GlossaryDraftPreviewModel
            {
                Draft = new GlossaryDraftModel
                {
                    Id = draft.Id,
                    BaseVersion = baseRelease.Version,
                    Status = draft.Status,
                    Version = publishedRelease?.Version ?? $"{nextVersion}-alpha",
                    ChangeType = publishedRelease is null ? change.ToString() : PublishedStatus,
                    CreatedUtc = draft.CreatedUtc,
                    UpdatedUtc = draft.UpdatedUtc,
                },
                Terms = terms,
                TermChanges = termChanges,
            };
        }

        private static List<GlossaryDraftTermChangeModel> DescribeTermChanges(
            IEnumerable<GlossaryModel> baseTerms,
            IEnumerable<GlossaryModel> terms,
            IReadOnlySet<Guid> explicitlyBreakingTermIds,
            GlossaryTermVersionIndex latestTermVersions,
            bool projectVersions
        )
        {
            var previousTerms = baseTerms.ToList();
            var currentTerms = terms.ToList();
            var previousById = previousTerms.ToDictionary(item => Guid.Parse(item.StaticId));
            var previousByName = previousTerms.ToDictionary(
                item => item.Name,
                StringComparer.OrdinalIgnoreCase
            );
            var representedPreviousIds = new HashSet<Guid>();
            var changes = new List<GlossaryDraftTermChangeModel>();

            foreach (var term in currentTerms)
            {
                var termId = Guid.Parse(term.StaticId);
                var matchedById = previousById.TryGetValue(termId, out var previous);
                if (!matchedById)
                {
                    previousByName.TryGetValue(term.Name, out previous);
                }

                if (previous is null)
                {
                    if (projectVersions)
                    {
                        term.Version = latestTermVersions.GetLatestVersion(term) + 1;
                    }

                    changes.Add(
                        new GlossaryDraftTermChangeModel
                        {
                            Term = term,
                            ChangeType = explicitlyBreakingTermIds.Contains(termId)
                                ? "Breaking Change"
                                : "New",
                            BreakingChange = explicitlyBreakingTermIds.Contains(termId),
                        }
                    );
                    continue;
                }

                var previousId = Guid.Parse(previous.StaticId);
                representedPreviousIds.Add(previousId);
                var contentChanged = !string.Equals(
                    GlossaryTermPersistence.GetContentHash(previous),
                    GlossaryTermPersistence.GetContentHash(term),
                    StringComparison.Ordinal
                );
                if (matchedById && !contentChanged)
                {
                    term.Version = previous.Version;
                    continue;
                }

                if (projectVersions)
                {
                    term.Version = Math.Max(
                        latestTermVersions.GetLatestVersion(term),
                        matchedById ? previous.Version : 0
                    ) + 1;
                }

                var isBreaking =
                    !matchedById
                    || explicitlyBreakingTermIds.Contains(termId)
                    || GlossaryChangeAnalyzer.Analyze(
                        [previous],
                        [term],
                        explicitlyBreakingTermIds
                    ) == GlossaryChangeKind.Major;
                changes.Add(
                    new GlossaryDraftTermChangeModel
                    {
                        Term = term,
                        OriginalTerm = previous,
                        ChangeType = isBreaking ? "Breaking Change" : "Bugfix",
                        BreakingChange = explicitlyBreakingTermIds.Contains(termId),
                    }
                );
            }

            changes.AddRange(
                previousTerms
                    .Where(term =>
                        !representedPreviousIds.Contains(Guid.Parse(term.StaticId))
                    )
                    .Select(term => new GlossaryDraftTermChangeModel
                    {
                        Term = term,
                        OriginalTerm = term,
                        ChangeType = "Deleted",
                    })
            );

            return changes.OrderBy(item => item.Term.Name, StringComparer.Ordinal).ToList();
        }

        private static string SerializeTerms(IEnumerable<GlossaryModel> terms)
        {
            return JsonSerializer.Serialize(
                terms.OrderBy(item => item.Name, StringComparer.Ordinal),
                _serializerOptions
            );
        }

        private static List<GlossaryModel> DeserializeTerms(string value)
        {
            return JsonSerializer.Deserialize<List<GlossaryModel>>(value, _serializerOptions) ?? [];
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
                IsCurrent = glossary.CurrentReleaseId == release.Id,
            };
        }
    }
}
