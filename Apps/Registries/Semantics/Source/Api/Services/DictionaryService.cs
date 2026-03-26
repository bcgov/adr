namespace Adr.Semantics.Services
{
    using System.Collections.Generic;
    using Adr.Semantics.Models;
    using Adr.Semantics.Providers;
    using Microsoft.Extensions.Logging;

    public class DictionaryService : IDictionaryService
    {
        private readonly ILogger<DictionaryService> _logger;
        private readonly IDictionaryProvider _dictionaryProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryService"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        public DictionaryService(
            ILogger<DictionaryService> logger,
            IDictionaryProvider dictionaryProvider
        )
        {
            _logger = logger;
            _dictionaryProvider = dictionaryProvider;
        }

        /// <inheritdoc/>
        public IEnumerable<DictionaryModel> GetAll()
        {
            // TODO:
            return new List<DictionaryModel>();
        }
    }
}
