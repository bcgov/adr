namespace Adr.Semantics.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using Adr.Semantics.Configuration.Models;
    using Adr.Semantics.Mappers;
    using Adr.Semantics.Models;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Loads dictionary information by fetching OpenAPI specs from the URLs configured
    /// under the DictionarySources section of appsettings.
    /// </summary>
    public class OpenApiProvider : IDictionaryProvider
    {
        private const string SourcesConfigSection = "DictionarySources";
        private const string GlossaryBaseUrlConfigKey = "GlossaryBaseUrl";

        private readonly ILogger<OpenApiProvider> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly DictionaryMapper _mapper;
        private readonly List<DictionarySourceConfig> _sources;
        private readonly string _glossaryBaseUrl;

        private List<DictionaryModel>? _dictionaries;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenApiProvider"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        /// <param name="httpClientFactory">Factory used to create HTTP clients for fetching specs.</param>
        /// <param name="configuration">Application configuration providing the source list.</param>
        public OpenApiProvider(
            ILogger<OpenApiProvider> logger,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration
        )
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _mapper = new DictionaryMapper();
            _sources = LoadSources(configuration);
            _glossaryBaseUrl = (configuration[GlossaryBaseUrlConfigKey] ?? string.Empty)
                .TrimEnd('/');
            _dictionaries = null;
        }

        /// <inheritdoc/>
        public IEnumerable<DictionaryModel> GetAllDictionaries()
        {
            _dictionaries ??= LoadDictionaries();
            return _dictionaries;
        }

        private static List<DictionarySourceConfig> LoadSources(
            IConfiguration configuration
        )
        {
            var sourceConfiguration =
                configuration
                    .GetSection(SourcesConfigSection)
                    .Get<Dictionary<string, DictionarySourceConfig>>()
                ?? new Dictionary<string, DictionarySourceConfig>();

            return sourceConfiguration
                .Where(config =>
                    config.Value is not null
                    && config.Value.Enabled
                    && !string.IsNullOrWhiteSpace(config.Value.SourceUrl)
                )
                .Select(config =>
                {
                    config.Value.Name = config.Key;
                    return config.Value;
                })
                .ToList();
        }

        private List<DictionaryModel> LoadDictionaries()
        {
            var entries = new List<DictionaryEntryModel>();
            var client = _httpClientFactory.CreateClient();

            foreach (var source in _sources)
            {
                var entry = FetchAndMap(client, source);
                if (entry is not null)
                {
                    entries.Add(entry);
                }
            }

            var dictionary = new DictionaryModel { Id = "openapi_based", Entries = entries };

            return new List<DictionaryModel> { dictionary };
        }

        private DictionaryEntryModel? FetchAndMap(HttpClient client, DictionarySourceConfig source)
        {
            try
            {
                _logger.LogInformation(
                    "Fetching OpenAPI spec for {Name} from {Url}",
                    source.Name,
                    source.SourceUrl
                );
                var json = client.GetStringAsync(source.SourceUrl).GetAwaiter().GetResult();
                var entry = _mapper.Map(source.Name, source.SourceUrl, json);
                ResolveSemanticTermRefs(entry);
                return entry;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed to fetch or parse OpenAPI spec for {Name} from {Url}",
                    source.Name,
                    source.SourceUrl
                );
                return null;
            }
        }

        private void ResolveSemanticTermRefs(DictionaryEntryModel entry)
        {
            foreach (var field in entry.Fields)
            {
                field.SemanticTermRef = BuildGlossaryHref(field.SemanticTermRef);
            }
        }

        private string BuildGlossaryHref(string termId)
        {
            if (string.IsNullOrWhiteSpace(termId))
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(_glossaryBaseUrl))
            {
                _logger.LogWarning(
                    "Field references glossary term {TermId} but {ConfigKey} is not configured; returning bare id.",
                    termId,
                    GlossaryBaseUrlConfigKey
                );
                return termId;
            }

            return $"{_glossaryBaseUrl}/{termId}";
        }
    }
}
