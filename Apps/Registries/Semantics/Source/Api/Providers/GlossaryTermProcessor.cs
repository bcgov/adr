namespace Adr.Semantics.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using Adr.Semantics.Mappers;
    using Adr.Semantics.Models;
    using Microsoft.Extensions.Logging;

    internal sealed class GlossaryTermProcessor
    {
        private static readonly Regex _slugPattern = new(
            "^[a-z0-9]+(?:-[a-z0-9]+)*$",
            RegexOptions.CultureInvariant
        );

        private readonly ILogger<GlossaryTermProcessor> _logger;

        public GlossaryTermProcessor(ILogger<GlossaryTermProcessor> logger)
        {
            _logger = logger;
        }

        public GlossaryTermProcessingResult Process(
            IEnumerable<GlossaryTermSubmission> submissions,
            IEnumerable<GlossaryModel> currentTerms,
            string sourceReference,
            IEnumerable<GlossaryModel>? knownTerms = null
        )
        {
            var current = currentTerms.ToList();
            var knownByName = current
                .Concat(knownTerms ?? [])
                .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
            var currentByName = current.ToDictionary(
                item => item.Name,
                StringComparer.OrdinalIgnoreCase
            );
            var currentById = current.ToDictionary(item => Guid.Parse(item.StaticId));
            var accepted = new List<GlossaryModel>();
            var attempts = new List<GlossaryTermProcessingAttempt>();
            var acceptedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var acceptedIds = new HashSet<Guid>();
            var carriedForwardIds = new HashSet<Guid>();

            foreach (var submission in submissions.OrderBy(item => item.Sequence))
            {
                var term = submission.Term;
                var submittedStaticId = term?.StaticId ?? submission.SubmittedStaticId;
                var errors = submission.Errors.ToList();
                Guid? resolvedStaticId = null;

                if (term is not null)
                {
                    if (submission.RequiresSchemaNormalization)
                    {
                        NormalizeSchemaMetadata(term, sourceReference);
                    }
                    resolvedStaticId = ResolveStaticId(term, knownByName, sourceReference);
                    Validate(term, acceptedNames, acceptedIds, errors);
                }

                if (errors.Count > 0 || term is null)
                {
                    var existing = FindExistingTerm(
                        term?.Name ?? submission.Name,
                        resolvedStaticId,
                        currentByName,
                        currentById
                    );
                    if (existing is not null && Guid.TryParse(existing.StaticId, out var existingId))
                    {
                        resolvedStaticId = existingId;
                        carriedForwardIds.Add(existingId);
                    }

                    LogInvalid(submission.Sequence, sourceReference, errors);
                    attempts.Add(
                        new GlossaryTermProcessingAttempt(
                            submission.Sequence,
                            term?.Name ?? submission.Name,
                            submittedStaticId,
                            resolvedStaticId,
                            term,
                            submission.SourcePayload,
                            submission.BreakingChange,
                            false,
                            errors
                        )
                    );
                    continue;
                }

                accepted.Add(term);
                acceptedNames.Add(term.Name);
                acceptedIds.Add(resolvedStaticId!.Value);
                attempts.Add(
                    new GlossaryTermProcessingAttempt(
                        submission.Sequence,
                        term.Name,
                        submittedStaticId,
                        resolvedStaticId,
                        term,
                        submission.SourcePayload,
                        submission.BreakingChange,
                        true,
                        []
                    )
                );
            }

            var acceptedIdsInSnapshot = accepted
                .Select(item => Guid.Parse(item.StaticId))
                .ToHashSet();
            var acceptedNamesInSnapshot = accepted
                .Select(item => item.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var effectiveTerms = accepted.ToList();
            effectiveTerms.AddRange(
                current.Where(item =>
                    Guid.TryParse(item.StaticId, out var id)
                    && carriedForwardIds.Contains(id)
                    && !acceptedIdsInSnapshot.Contains(id)
                    && !acceptedNamesInSnapshot.Contains(item.Name)
                )
            );

            return new GlossaryTermProcessingResult(attempts, accepted, effectiveTerms);
        }

        internal void NormalizeSchemaMetadata(GlossaryModel term, string sourceReference)
        {
            if (!GlossarySchemaType.IsSupported(term.SchemaType))
            {
                _logger.LogWarning(
                    "UNSUPPORTED SCHEMA TYPE {SchemaType} FOR TERM {TermName} IN GLOSSARY SOURCE {GlossarySource}; FALLING BACK TO STRING.",
                    term.SchemaType,
                    term.Name,
                    sourceReference
                );
            }

            term.SchemaType = GlossarySchemaType.Normalize(term.SchemaType);
            var result = GlossarySchemaConstraints.Parse(
                term.SchemaConstraintsSource,
                term.SchemaType
            );
            term.SchemaConstraints = result.Constraints;

            foreach (var issue in result.Issues)
            {
                _logger.LogWarning(
                    "INVALID SCHEMA CONSTRAINT {ConstraintName} FOR TERM {TermName} IN GLOSSARY SOURCE {GlossarySource}: {Reason}; CONSTRAINT EXCLUDED.",
                    issue.ConstraintName,
                    term.Name,
                    sourceReference,
                    issue.Reason
                );
            }
        }

        internal Guid ResolveStaticId(
            GlossaryModel term,
            Dictionary<string, GlossaryModel> currentTermsByName,
            string sourceReference
        )
        {
            if (!string.IsNullOrWhiteSpace(term.StaticId))
            {
                return Guid.TryParse(term.StaticId, out var suppliedId) ? suppliedId : Guid.Empty;
            }

            if (currentTermsByName.TryGetValue(term.Name, out var existing))
            {
                term.StaticId = existing.StaticId;
                _logger.LogInformation(
                    "Reused StaticId {StaticId} for glossary term {TermName} in {GlossarySource}.",
                    term.StaticId,
                    term.Name,
                    sourceReference
                );
            }
            else
            {
                term.StaticId = Guid.NewGuid().ToString();
                _logger.LogInformation(
                    "Generated StaticId {StaticId} for new glossary term {TermName} in {GlossarySource}.",
                    term.StaticId,
                    term.Name,
                    sourceReference
                );
            }

            return Guid.Parse(term.StaticId);
        }

        private static void Validate(
            GlossaryModel term,
            HashSet<string> acceptedNames,
            HashSet<Guid> acceptedIds,
            List<string> errors
        )
        {
            if (string.IsNullOrWhiteSpace(term.Term))
            {
                errors.Add("Term is required");
            }

            if (string.IsNullOrWhiteSpace(term.Name))
            {
                errors.Add("Name is required");
            }
            else if (!_slugPattern.IsMatch(term.Name))
            {
                errors.Add($"Name '{term.Name}' is not a lowercase hyphenated slug");
            }
            else if (acceptedNames.Contains(term.Name))
            {
                errors.Add($"Name '{term.Name}' was already accepted from an earlier submission");
            }

            if (string.IsNullOrWhiteSpace(term.Definition))
            {
                errors.Add("Published Definition is required");
            }

            if (!Guid.TryParse(term.StaticId, out var staticId))
            {
                errors.Add($"StaticId '{term.StaticId}' is not a GUID");
            }
            else if (acceptedIds.Contains(staticId))
            {
                errors.Add(
                    $"StaticId '{term.StaticId}' was already accepted from an earlier submission"
                );
            }

            if (
                !string.IsNullOrWhiteSpace(term.Example)
                && !ExampleMatchesSchemaType(term.Example, term.SchemaType)
            )
            {
                errors.Add($"Example '{term.Example}' is not a valid {term.SchemaType}");
            }
        }

        private static GlossaryModel? FindExistingTerm(
            string name,
            Guid? resolvedStaticId,
            Dictionary<string, GlossaryModel> currentByName,
            Dictionary<Guid, GlossaryModel> currentById
        )
        {
            if (!string.IsNullOrWhiteSpace(name) && currentByName.TryGetValue(name, out var byName))
            {
                return byName;
            }

            return resolvedStaticId is not null
                && currentById.TryGetValue(resolvedStaticId.Value, out var byId)
                ? byId
                : null;
        }

        private void LogInvalid(int sequence, string sourceReference, IReadOnlyCollection<string> errors)
        {
            _logger.LogCritical(
                "GLOSSARY TERM SUBMISSION {Sequence} IN {GlossarySource} IS INVALID AND WILL NOT BE PUBLISHED: {Reasons}",
                sequence,
                sourceReference,
                string.Join("; ", errors.DefaultIfEmpty("The term could not be read"))
            );
        }

        private static bool ExampleMatchesSchemaType(string example, string schemaType)
        {
            return schemaType.ToLowerInvariant() switch
            {
                "number" => decimal.TryParse(
                    example,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out _
                ),
                "integer" => long.TryParse(
                    example,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out _
                ),
                "boolean" => bool.TryParse(example, out _),
                _ => true,
            };
        }
    }

    internal sealed record GlossaryTermSubmission(
        int Sequence,
        GlossaryModel? Term,
        string Name,
        string SubmittedStaticId,
        string SourcePayload,
        bool BreakingChange,
        IReadOnlyList<string> Errors,
        bool RequiresSchemaNormalization
    )
    {
        public static GlossaryTermSubmission FromTerm(
            int sequence,
            GlossaryModel term,
            bool breakingChange = false,
            string? sourcePayload = null
        )
        {
            return new GlossaryTermSubmission(
                sequence,
                term,
                term.Name,
                term.StaticId,
                sourcePayload ?? JsonSerializer.Serialize(term),
                breakingChange,
                [],
                true
            );
        }

        public static GlossaryTermSubmission FromExistingTerm(int sequence, GlossaryModel term)
        {
            return new GlossaryTermSubmission(
                sequence,
                term,
                term.Name,
                term.StaticId,
                JsonSerializer.Serialize(term),
                false,
                [],
                false
            );
        }
    }

    internal sealed record GlossaryTermProcessingAttempt(
        int Sequence,
        string Name,
        string SubmittedStaticId,
        Guid? ResolvedStaticId,
        GlossaryModel? Term,
        string SourcePayload,
        bool BreakingChange,
        bool IsValid,
        IReadOnlyList<string> InvalidReasons
    );

    internal sealed record GlossaryTermProcessingResult(
        IReadOnlyList<GlossaryTermProcessingAttempt> Attempts,
        IReadOnlyList<GlossaryModel> AcceptedTerms,
        IReadOnlyList<GlossaryModel> EffectiveTerms
    );
}
