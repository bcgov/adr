namespace Adr.Semantics.Providers
{
    using System.Collections.Generic;
    using Adr.Semantics.Models;
    using Microsoft.Extensions.Logging;

    public class OpenApiProvider : IDictionaryProvider
    {
        private readonly ILogger<OpenApiProvider> _logger;

        // TODO
        private IEnumerable<DictionaryModel>? _dictionaries;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenApiProvider"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        public OpenApiProvider(ILogger<OpenApiProvider> logger)
        {
            _logger = logger;
            _dictionaries = null;
        }

        /// <inheritdoc/>
        public IEnumerable<DictionaryModel> GetAllDictionaries()
        {
            return new List<DictionaryModel>();
        }
    }
}
