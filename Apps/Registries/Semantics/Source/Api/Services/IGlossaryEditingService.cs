namespace Adr.Semantics.Services
{
    using System;
    using System.Collections.Generic;
    using Adr.Semantics.Models;

    /// <summary>
    /// Manages draft changes to the current glossary release.
    /// </summary>
    public interface IGlossaryEditingService
    {
        /// <summary>Creates a draft from the current release.</summary>
        GlossaryDraftPreviewModel CreateDraft();

        /// <summary>Gets draft metadata, optionally filtered by status.</summary>
        IList<GlossaryDraftModel> GetDrafts(string? status = null);

        /// <summary>Gets a draft and its effective content.</summary>
        GlossaryDraftPreviewModel? GetDraft(Guid draftId);

        /// <summary>Submits a term update to a draft.</summary>
        GlossaryTermSubmissionResultModel? PutTerm(
            Guid draftId,
            string name,
            GlossaryTermEditModel edit
        );

        /// <summary>Deletes a term from a draft.</summary>
        GlossaryTermSubmissionResultModel? DeleteTerm(Guid draftId, string name);

        /// <summary>Applies a stale draft's changes to the current glossary release.</summary>
        GlossaryDraftRebaseResultModel? Rebase(Guid draftId);

        /// <summary>Publishes a draft as a new glossary release.</summary>
        GlossaryDraftPublishResultModel? Publish(Guid draftId, bool ignoreInvalid);
    }

    /// <summary>
    /// Indicates that a glossary draft operation conflicts with the draft lifecycle.
    /// </summary>
    internal sealed class GlossaryDraftConflictException : InvalidOperationException
    {
        /// <summary>Initializes a new instance with the conflict detail.</summary>
        public GlossaryDraftConflictException(string message)
            : base(message) { }
    }
}
