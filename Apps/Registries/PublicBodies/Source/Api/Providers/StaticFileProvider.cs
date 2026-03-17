namespace Adr.PublicBodies.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Adr.PublicBodies.Mappers;
    using Adr.PublicBodies.Models;
    using CsvHelper;
    using CsvHelper.Configuration;
    using CsvHelper.TypeConversion;
    using Microsoft.Extensions.Logging;

    public class StaticFileProvider : IPublicBodyProvider
    {
        private readonly ILogger<StaticFileProvider> _logger;

        private IEnumerable<PublicBodyModel>? _publicBodies;
        private IEnumerable<PublicBodyTypeModel>? _publicBodyTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFileProvider"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        public StaticFileProvider(ILogger<StaticFileProvider> logger)
        {
            _logger = logger;
            _publicBodies = null;
            _publicBodyTypes = null;
        }

        /// <inheritdoc/>
        public IEnumerable<PublicBodyModel> GetAllPublicBodies()
        {
            if (_publicBodies is null)
            {
                _publicBodies = LoadPublicBodiesFromFile();
            }

            return _publicBodies;
        }

        public IEnumerable<PublicBodyTypeModel> GetAllTypes()
        {
            if (_publicBodyTypes is null)
            {
                _publicBodyTypes = LoadTypesFromFile();
            }

            return _publicBodyTypes;
        }

        private IEnumerable<PublicBodyModel> LoadPublicBodiesFromFile()
        {
            _logger.LogInformation("Parsing Names file");

            var publicBodiesFile = "public bodies_ministry_plus_public_bodies.csv";

            var mapper = new PublicBodyMapper();
            var records = LoadAsset<PublicBodyModel>(publicBodiesFile, mapper);

            // For now generate the ID on the fly
            foreach (var nameRecord in records)
            {
                nameRecord.Id = Guid.NewGuid().ToString();
            }
            return records;
        }

        private IEnumerable<PublicBodyTypeModel> LoadTypesFromFile()
        {
            _logger.LogInformation("Parsing Types file");

            var typesFileName = "public bodies_ministry_plus_public_body_types.csv";

            var mapper = new PublicBodyTypeMapper();
            var records = LoadAsset<PublicBodyTypeModel>(typesFileName, mapper);

            // For now generate the ID on the fly
            foreach (var typeRecord in records)
            {
                typeRecord.Id = Guid.NewGuid().ToString();
            }
            return records;
        }

        private IEnumerable<T> LoadAsset<T>(string assetName, ClassMap mapper)
        {
            string resourceName = $"PublicBodies.Assets.{assetName}";

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
        }
    }
}
