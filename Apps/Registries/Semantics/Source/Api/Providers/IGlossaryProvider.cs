namespace Adr.Semantics.Providers
{
    using System;
    using System.Collections.Generic;
    using Adr.Semantics.Models;

    /// <summary>
    /// Interface for a provider that provides glossary information.
    /// </summary>
    public interface IGlossaryProvider
    {
        /// <summary>
        /// Gets all terms in the current glossary release.
        /// </summary>
        /// <returns>A list of glossary terms.</returns>
        IEnumerable<GlossaryModel> GetAllGlossaries();

        /// <summary>
        /// Gets all terms in a specific glossary release.
        /// </summary>
        /// <param name="version">The glossary semantic version.</param>
        /// <returns>A list of glossary terms, or <c>null</c> when the version does not exist.</returns>
        IEnumerable<GlossaryModel>? GetAllGlossaries(string version);

        /// <summary>
        /// Gets the current glossary release metadata.
        /// </summary>
        /// <returns>The current release metadata.</returns>
        GlossaryVersionModel GetCurrentVersion();

        /// <summary>
        /// Gets metadata for every available glossary release.
        /// </summary>
        /// <returns>The available releases.</returns>
        IEnumerable<GlossaryVersionModel> GetVersions();

        /// <summary>
        /// Gets metadata for a specific glossary release.
        /// </summary>
        /// <param name="version">The glossary semantic version.</param>
        /// <returns>The release metadata, or <c>null</c> when the version does not exist.</returns>
        GlossaryVersionModel? GetVersion(string version);

        /// <summary>
        /// Gets every persisted version of the current term identified by its slug.
        /// </summary>
        /// <param name="term">The current human-readable term slug.</param>
        /// <returns>The term versions, newest first, or <c>null</c> when the term does not exist.</returns>
        IEnumerable<GlossaryModel>? GetTermVersions(string term);

        /// <summary>
        /// Gets a persisted version of the current term identified by its slug.
        /// </summary>
        /// <param name="term">The current human-readable term slug.</param>
        /// <param name="termVersion">The monotonically increasing term version.</param>
        /// <returns>The requested term version, or <c>null</c> when it does not exist.</returns>
        GlossaryModel? GetTermVersion(string term, int termVersion);

        /// <summary>
        /// Gets the current valid glossary term identified by its stable UUID.
        /// </summary>
        /// <param name="id">The stable term UUID.</param>
        /// <returns>The current term, or <c>null</c> when it is not in the current release.</returns>
        GlossaryModel? GetGlossaryEntryById(Guid id);

        /// <summary>
        /// Gets published revisions and invalid submissions for a term selected from a release.
        /// </summary>
        /// <param name="glossaryVersion">The glossary release containing the term.</param>
        /// <param name="term">The term slug as it appears in that release.</param>
        /// <returns>The term history, newest first, or <c>null</c> when the release or term does not exist.</returns>
        IEnumerable<GlossaryTermHistoryModel>? GetTermHistory(
            string glossaryVersion,
            string term
        );

    }
}
