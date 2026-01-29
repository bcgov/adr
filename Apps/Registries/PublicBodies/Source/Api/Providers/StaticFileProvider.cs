namespace Adr.PublicBodies.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using Adr.PublicBodies.Mappers;
    using Adr.PublicBodies.Models;
    using CsvHelper;
    using CsvHelper.Configuration;
    using CsvHelper.TypeConversion;
    using Microsoft.Extensions.Logging;

    public class StaticFileProvider : IPublicBodyProvider
    {
        private readonly ILogger<StaticFileProvider> _logger;

        private IEnumerable<PublicBodyNameModel>? _publicBodyNames;
        private IEnumerable<PublicBodyTypeModel>? _publicBodyTypes;

        /// <summary>
        /// Initializes a new instance of the <see cref="StaticFileProvider"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        public StaticFileProvider(ILogger<StaticFileProvider> logger)
        {
            _logger = logger;
            _publicBodyNames = null;
            _publicBodyTypes = null;
        }

        /// <inheritdoc/>
        public IEnumerable<PublicBodyNameModel> GetAllNames()
        {
            if (_publicBodyNames is null)
            {
                _publicBodyNames = LoadNamesFromFile();
            }

            return _publicBodyNames;
        }

        public IEnumerable<PublicBodyTypeModel> GetAllTypes()
        {
            if (_publicBodyTypes is null)
            {
                _publicBodyTypes = LoadTypesFromFile();
            }

            return _publicBodyTypes;
        }

        private IEnumerable<PublicBodyNameModel> LoadNamesFromFile()
        {
            _logger.LogInformation("Parsing Names file");

            var namesFileName = "public body names 2026-01-21v2.csv";

            using StreamReader reader = new("Assets/" + namesFileName);
            CsvConfiguration csvConfig = new(CultureInfo.InvariantCulture);
            csvConfig.Delimiter = "\t";
            using CsvReader csv = new(reader, csvConfig);
            csv.Context.TypeConverterCache.AddConverter<bool>(new BooleanConverter());
            PublicBodyNameMapper mapper = new();
            csv.Context.RegisterClassMap(mapper);
            IEnumerable<PublicBodyNameModel> records = csv.GetRecords<PublicBodyNameModel>()
                .ToList();

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

            var typesFileName = "public body types 2026-01-21v2.csv";

            using StreamReader reader = new("Assets/" + typesFileName);
            CsvConfiguration csvConfig = new(CultureInfo.InvariantCulture);
            csvConfig.Delimiter = "\t";
            using CsvReader csv = new(reader, csvConfig);
            csv.Context.TypeConverterCache.AddConverter<bool>(new BooleanConverter());
            PublicBodyTypeMapper mapper = new();
            csv.Context.RegisterClassMap(mapper);
            IEnumerable<PublicBodyTypeModel> records = csv.GetRecords<PublicBodyTypeModel>()
                .ToList();

            // For now generate the ID on the fly
            foreach (var typeRecord in records)
            {
                typeRecord.Id = Guid.NewGuid().ToString();
            }
            return records;
        }
    }
}
