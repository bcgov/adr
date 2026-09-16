namespace Adr.Semantics.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Adr.Semantics.Models;
    using Adr.Semantics.Providers;
    using Microsoft.Extensions.Logging;

    public class GlossaryService : IGlossaryService
    {
        private readonly ILogger<GlossaryService> _logger;
        private readonly IGlossaryProvider _glossaryProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlossaryService"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        /// <param name="glossaryProvider">Glossary data provider.</param>
        public GlossaryService(ILogger<GlossaryService> logger, IGlossaryProvider glossaryProvider)
        {
            _logger = logger;
            _glossaryProvider = glossaryProvider;
        }

        /// <inheritdoc/>
        public IEnumerable<GlossaryModel> GetAll()
        {
            return _glossaryProvider.GetAllGlossaries();
        }

        /// <inheritdoc/>
        public IEnumerable<GlossaryModel>? GetAll(string version)
        {
            return _glossaryProvider.GetAllGlossaries(version);
        }

        /// <inheritdoc/>
        public GlossaryVersionModel GetCurrentVersion()
        {
            return _glossaryProvider.GetCurrentVersion();
        }

        /// <inheritdoc/>
        public IEnumerable<GlossaryVersionModel> GetVersions()
        {
            return _glossaryProvider.GetVersions();
        }

        /// <inheritdoc/>
        public GlossaryVersionModel? GetVersion(string version)
        {
            return _glossaryProvider.GetVersion(version);
        }

        /// <inheritdoc/>
        public IEnumerable<GlossaryModel>? GetTermVersions(string term)
        {
            return _glossaryProvider.GetTermVersions(term);
        }

        /// <inheritdoc/>
        public GlossaryModel? GetTermVersion(string term, int termVersion)
        {
            return _glossaryProvider.GetTermVersion(term, termVersion);
        }

        /// <inheritdoc/>
        public GlossaryModel? GetGlossaryEntryById(Guid id)
        {
            return _glossaryProvider.GetGlossaryEntryById(id);
        }

        /// <inheritdoc/>
        public IEnumerable<GlossaryTermHistoryModel>? GetTermHistory(
            string glossaryVersion,
            string term
        )
        {
            return _glossaryProvider.GetTermHistory(glossaryVersion, term);
        }

        /// <inheritdoc/>
        public GlossaryModel? GetGlossaryEntryByTerm(string term)
        {
            var glossaries = GetAll();
            return glossaries?.FirstOrDefault(x => x?.Name == term);
        }

        /// <inheritdoc/>
        public GlossaryModel? GetGlossaryEntryByTerm(string term, string version)
        {
            var glossaries = GetAll(version);
            return glossaries?.FirstOrDefault(x => x?.Name == term);
        }
    }
}
