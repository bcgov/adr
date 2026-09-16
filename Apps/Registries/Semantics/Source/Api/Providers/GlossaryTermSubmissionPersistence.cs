namespace Adr.Semantics.Providers
{
    using System;
    using System.Text.Json;
    using Adr.Semantics.Data;

    internal static class GlossaryTermSubmissionPersistence
    {
        public static GlossaryTermSubmissionEntity ToEntity(
            string glossaryId,
            string sourceType,
            string sourceReference,
            string operation,
            GlossaryTermProcessingAttempt attempt,
            GlossaryTermRevisionEntity? revision = null
        )
        {
            return new GlossaryTermSubmissionEntity
            {
                GlossaryId = glossaryId,
                Sequence = attempt.Sequence,
                SourceType = sourceType,
                SourceReference = sourceReference,
                Operation = operation,
                Name = attempt.Name,
                SubmittedStaticId = attempt.SubmittedStaticId,
                ResolvedStaticId = attempt.ResolvedStaticId,
                SourcePayload = attempt.SourcePayload,
                BreakingChange = attempt.BreakingChange,
                IsValid = attempt.IsValid,
                InvalidReasonsJson = JsonSerializer.Serialize(attempt.InvalidReasons),
                TermRevision = revision,
                SubmittedUtc = DateTime.UtcNow,
            };
        }
    }
}
