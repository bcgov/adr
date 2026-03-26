namespace Adr.Semantics.Providers
{
    using System.Collections.Generic;
    using Adr.Semantics.Models;
    using CsvHelper.Configuration;
    using Microsoft.Extensions.Logging;

    public class GlossaryStaticFileProvider : IGlossaryProvider
    {
        private readonly ILogger<GlossaryStaticFileProvider> _logger;

        // TODO
        private IEnumerable<GlossaryModel>? _glossaries;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFileProvider"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        public GlossaryStaticFileProvider(ILogger<GlossaryStaticFileProvider> logger)
        {
            _logger = logger;
            _glossaries = null;
        }

        /// <inheritdoc/>
        public IEnumerable<GlossaryModel> GetAllGlossaries()
        {
            if (_glossaries is null)
            {
                _glossaries = LoadGlossaryFromFile();
            }

            return _glossaries;
        }

        private IEnumerable<GlossaryModel> LoadGlossaryFromFile()
        {
            _logger.LogInformation("Parsing Names file");

            /*
             * TODO: retrieve from CSV the correct info
            var glossaryFile = "public bodies_ministry_plus_public_bodies.csv";

            var mapper = new GlossaryMapper();
            var records = LoadAsset<GlossaryModel>(glossaryFile, mapper);

            // For now generate the ID on the fly
            foreach (var nameRecord in records)
            {
                nameRecord.Id = Guid.NewGuid().ToString();
            }
            return records;
            */

            return new List<GlossaryModel>();
        }

        private IEnumerable<T> LoadAsset<T>(string assetName, ClassMap mapper)
        {
            /*
             * TODO load the actual file and parse it
              string resourceName = $"Semantic.Assets.{assetName}";
  
              Assembly? assembly = Assembly.GetAssembly(typeof(StaticFileProvider));
  
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
              */
            return new List<T>();
        }
    }
}
