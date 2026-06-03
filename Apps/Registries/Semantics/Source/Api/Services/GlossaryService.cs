namespace Adr.Semantics.Services
{
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
        public GlossaryModel? GetGlossaryEntryByTerm(string term)
        {
            var glossaries = GetAll();
            return glossaries?.FirstOrDefault(x => x?.Name == term);
        }
    }
}
