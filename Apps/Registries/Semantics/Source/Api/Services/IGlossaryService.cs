namespace Adr.Semantics.Services
{
    using System;
    using System.Collections.Generic;
    using Adr.Semantics.Models;

    /// <summary>
    /// Interface for a service that manages Glossary information.
    /// </summary>
    public interface IGlossaryService
    {
        /// <summary>
        /// Gets all the glossaries.
        /// </summary>
        /// <returns>A list glossaries.</returns>
        IEnumerable<GlossaryModel> GetAll();

        /// <summary>
        /// Gets all glossary terms for a specific glossary release.
        /// </summary>
        /// <param name="version">The glossary semantic version.</param>
        /// <returns>A list of glossary terms, or <c>null</c> if the version does not exist.</returns>
        IEnumerable<GlossaryModel>? GetAll(string version);

        /// <summary>
        /// Gets the current glossary release metadata.
        /// </summary>
        /// <returns>The current release metadata.</returns>
        GlossaryVersionModel GetCurrentVersion();

        /// <summary>
        /// Gets all available glossary releases.
        /// </summary>
        /// <returns>The available release metadata.</returns>
        IEnumerable<GlossaryVersionModel> GetVersions();

        /// <summary>
        /// Gets metadata for a specific glossary release.
        /// </summary>
        /// <param name="version">The glossary semantic version.</param>
        /// <returns>The release metadata, or <c>null</c> if the version does not exist.</returns>
        GlossaryVersionModel? GetVersion(string version);

        /// <summary>
        /// Gets all persisted versions of the current term identified by its slug.
        /// </summary>
        /// <param name="term">The current human-readable term slug.</param>
        /// <returns>The term versions, newest first, or <c>null</c> when the term does not exist.</returns>
        IEnumerable<GlossaryModel>? GetTermVersions(string term);

        /// <summary>
        /// Gets a persisted version of the current term identified by its slug.
        /// </summary>
        /// <param name="term">The current human-readable term slug.</param>
        /// <param name="termVersion">The term version.</param>
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

        /// <summary>
        /// Gets the glossary for a given term.
        /// </summary>
        /// <returns>A glossaries entry, or <c>null</c> if no entry matches the supplied term.</returns>
        GlossaryModel? GetGlossaryEntryByTerm(string term);

        /// <summary>
        /// Gets a glossary term from a specific release by its human-readable slug.
        /// </summary>
        /// <param name="term">The term slug.</param>
        /// <param name="version">The glossary semantic version.</param>
        /// <returns>A glossary entry, or <c>null</c> if no entry matches.</returns>
        GlossaryModel? GetGlossaryEntryByTerm(string term, string version);

    }
}
