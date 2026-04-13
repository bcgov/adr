namespace Adr.Semantics.Providers
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Adr.Semantics.Mappers;
    using Adr.Semantics.Models;
    using CsvHelper;
    using CsvHelper.Configuration;
    using CsvHelper.TypeConversion;
    using Microsoft.Extensions.Logging;

    public class GlossaryStaticFileProvider : IGlossaryProvider
    {
        private readonly ILogger<GlossaryStaticFileProvider> _logger;

        private IEnumerable<GlossaryModel>? _glossaryTerms;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFileProvider"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        public GlossaryStaticFileProvider(ILogger<GlossaryStaticFileProvider> logger)
        {
            _logger = logger;
            _glossaryTerms = null;
        }

        /// <inheritdoc/>
        public IEnumerable<GlossaryModel> GetAllGlossaries()
        {
            if (_glossaryTerms is null)
            {
                _glossaryTerms = LoadGlossaryFromFile();
            }

            return _glossaryTerms;
        }

        private IEnumerable<GlossaryModel> LoadGlossaryFromFile()
        {
            _logger.LogInformation("Parsing Names file");

            var glossaryFile = "Glossary_of_Terms_Data_Table.csv";

            var mapper = new GlossaryMapper();
            var records = LoadAsset<GlossaryModel>(glossaryFile, mapper);

            // For now generate the ID on the fly
            foreach (var nameRecord in records)
            {
                // For now use the lower case term separated by dashes as identifier
                // "API System" -> "api-system"
                nameRecord.Id = nameRecord.Term.Replace(" ", "-").ToLowerInvariant();
            }

            return records;
        }

        private static IEnumerable<T> LoadAsset<T>(string assetName, ClassMap mapper)
        {
            string resourceName = $"Semantics.Assets.{assetName}";

            Assembly? assembly = Assembly.GetAssembly(typeof(GlossaryStaticFileProvider));

            Stream resourceStream =
                assembly!.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"File {resourceName} not found.");
            using var reader = new StreamReader(resourceStream);

            CsvConfiguration csvConfig = new(CultureInfo.InvariantCulture);
            csvConfig.Delimiter = ",";
            using CsvReader csv = new(reader, csvConfig);
            csv.Context.TypeConverterCache.AddConverter<bool>(new BooleanConverter());
            csv.Context.RegisterClassMap(mapper);
            IEnumerable<T> records = csv.GetRecords<T>().ToList();

            return records;
        }
    }
}
