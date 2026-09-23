namespace Adr.Semantics.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text.Json;
    using Adr.Semantics.Data;
    using Adr.Semantics.Mappers;
    using Adr.Semantics.Models;
    using CsvHelper;
    using CsvHelper.Configuration;
    using CsvHelper.TypeConversion;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging;

    internal sealed class GlossaryImportService
    {
        private const string CatalogAsset = "GlossaryReleases.json";

        private static readonly JsonSerializerOptions _catalogSerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
        };

        private readonly IDbContextFactory<GlossaryDbContext> _contextFactory;
        private readonly ILogger<GlossaryImportService> _logger;
        private readonly GlossaryReleasePublisher _releasePublisher;
        private readonly GlossaryTermProcessor _termProcessor;

        public GlossaryImportService(
            IDbContextFactory<GlossaryDbContext> contextFactory,
            ILogger<GlossaryImportService> logger,
            GlossaryReleasePublisher releasePublisher,
            GlossaryTermProcessor termProcessor
        )
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _releasePublisher = releasePublisher;
            _termProcessor = termProcessor;
        }

        public void Initialize()
        {
            using var context = _contextFactory.CreateDbContext();
            context.Database.Migrate();

            var catalog = LoadCatalog();
            var glossary = context.Glossaries.SingleOrDefault(item => item.Id == catalog.Id);
            if (glossary is null)
            {
                glossary = new GlossaryEntity { Id = catalog.Id, Name = catalog.Name };
                context.Glossaries.Add(glossary);
                context.SaveChanges();
            }
            else if (!string.Equals(glossary.Name, catalog.Name, StringComparison.Ordinal))
            {
                glossary.Name = catalog.Name;
                context.SaveChanges();
            }

            for (var index = 0; index < catalog.Imports.Count; index++)
            {
                Import(context, glossary, catalog.Imports[index], index + 1);
                context.ChangeTracker.Clear();
                glossary = context.Glossaries.Single(item => item.Id == catalog.Id);
            }

            if (glossary.CurrentReleaseId is null)
            {
                _logger.LogCritical(
                    "NO VALID GLOSSARY IMPORTS WERE FOUND. APPLICATION STARTUP CANNOT CONTINUE."
                );
                throw new InvalidDataException("No valid glossary imports were found.");
            }
        }

        private void Import(
            GlossaryDbContext context,
            GlossaryEntity glossary,
            GlossaryImportDefinition definition,
            int sequence
        )
        {
            var bytes = ReadAsset(definition.Asset);
            var sourceHash = Convert.ToHexString(SHA256.HashData(bytes));
            var existingImport = context.GlossaryImports.SingleOrDefault(item =>
                item.GlossaryId == glossary.Id
                && item.SourceAsset == definition.Asset
                && item.SourceHash == sourceHash
            );
            if (existingImport is not null)
            {
                if (existingImport.Status == "Rejected")
                {
                    _logger.LogCritical(
                        "GLOSSARY IMPORT {GlossaryAsset} IS INVALID AND WAS SKIPPED: {Reason}",
                        definition.Asset,
                        existingImport.Error
                    );
                }

                return;
            }

            var priorAssetImport = context.GlossaryImports.FirstOrDefault(item =>
                item.GlossaryId == glossary.Id && item.SourceAsset == definition.Asset
            );
            if (priorAssetImport is not null)
            {
                throw new InvalidDataException(
                    $"Glossary import asset '{definition.Asset}' was modified after it was imported. Add a new asset instead."
                );
            }

            var lastImportedSequence = context
                .GlossaryImports.Where(item => item.GlossaryId == glossary.Id)
                .Select(item => (int?)item.Sequence)
                .Max();
            if (lastImportedSequence is not null && sequence <= lastImportedSequence)
            {
                throw new InvalidDataException(
                    $"Glossary import asset '{definition.Asset}' was inserted before an already processed import. Append new imports to the catalog."
                );
            }

            GlossaryTermProcessingResult? processingResult = null;
            try
            {
                var submissions = LoadSubmissions(bytes);
                processingResult = _termProcessor.Process(
                    submissions,
                    GlossaryTermQueries.LoadCurrentTerms(context, glossary),
                    definition.Asset,
                    GlossaryTermQueries.LoadKnownTerms(context, glossary.Id)
                );
                if (
                    !processingResult.EffectiveTerms.Any(item =>
                        item.PublishToDevHub && item.VerifiedDefinitionFlag
                    )
                )
                {
                    throw new InvalidDataException(
                        "The glossary contains no valid published, verified terms."
                    );
                }

                PersistImport(
                    context,
                    glossary,
                    definition,
                    sequence,
                    sourceHash,
                    processingResult
                );
            }
            catch (Exception exception)
                when (exception
                    is CsvHelperException
                        or FormatException
                        or IOException
                        or InvalidDataException)
            {
                context.ChangeTracker.Clear();
                var persistedGlossary = context.Glossaries.Single(item => item.Id == glossary.Id);
                var rejectedImport = CreateImport(
                    persistedGlossary.Id,
                    definition,
                    sequence,
                    sourceHash,
                    "Rejected",
                    exception.Message
                );
                if (processingResult is not null)
                {
                    foreach (var attempt in processingResult.Attempts)
                    {
                        rejectedImport.TermSubmissions.Add(
                            GlossaryTermSubmissionPersistence.ToEntity(
                                persistedGlossary.Id,
                                "CSV",
                                definition.Asset,
                                "Upsert",
                                attempt
                            )
                        );
                    }
                }

                context.GlossaryImports.Add(rejectedImport);
                context.SaveChanges();
                _logger.LogCritical(
                    exception,
                    "GLOSSARY IMPORT {GlossaryAsset} IS INVALID AND WILL BE SKIPPED.",
                    definition.Asset
                );
            }
        }

        private void PersistImport(
            GlossaryDbContext context,
            GlossaryEntity glossary,
            GlossaryImportDefinition definition,
            int importSequence,
            string sourceHash,
            GlossaryTermProcessingResult processingResult
        )
        {
            using var transaction = context.Database.BeginTransaction();
            var import = CreateImport(
                glossary.Id,
                definition,
                importSequence,
                sourceHash,
                "Imported"
            );
            var publication = _releasePublisher.Publish(
                    context,
                    glossary,
                    definition.PublishedAt,
                    definition.Asset,
                    sourceHash,
                    processingResult.EffectiveTerms,
                    processingResult.Attempts
                        .Where(item =>
                            item.IsValid
                            && item.BreakingChange
                            && item.ResolvedStaticId is not null
                        )
                        .Select(item => item.ResolvedStaticId!.Value)
                        .ToHashSet(),
                    forceReleaseBoundary: true
                )
                ?? throw new InvalidOperationException(
                    "A glossary import must always produce a release boundary."
                );

            foreach (var attempt in processingResult.Attempts)
            {
                var revision = attempt.IsValid && attempt.ResolvedStaticId is not null
                    ? publication.ReleaseRevisions.GetValueOrDefault(
                        attempt.ResolvedStaticId.Value
                    )
                    : null;
                import.TermSubmissions.Add(
                    GlossaryTermSubmissionPersistence.ToEntity(
                        glossary.Id,
                        "CSV",
                        definition.Asset,
                        "Upsert",
                        attempt,
                        revision
                    )
                );
            }
            import.ReleaseId = publication.Release.Id;
            context.GlossaryImports.Add(import);
            context.SaveChanges();
            transaction.Commit();

            _logger.LogInformation(
                "Imported glossary asset {GlossaryAsset} as version {GlossaryVersion} ({ChangeKind}).",
                definition.Asset,
                publication.Release.Version,
                publication.Change
            );
        }

        private static GlossaryImportEntity CreateImport(
            string glossaryId,
            GlossaryImportDefinition definition,
            int sequence,
            string sourceHash,
            string status,
            string? error = null,
            long? releaseId = null
        )
        {
            return new GlossaryImportEntity
            {
                GlossaryId = glossaryId,
                Sequence = sequence,
                PublishedAt = definition.PublishedAt,
                SourceAsset = definition.Asset,
                SourceHash = sourceHash,
                Status = status,
                Error = error,
                ReleaseId = releaseId,
                ImportedUtc = DateTime.UtcNow,
            };
        }

        private static GlossaryReleaseCatalog LoadCatalog()
        {
            using var stream = OpenAsset(CatalogAsset);
            var catalog = JsonSerializer.Deserialize<GlossaryReleaseCatalog>(
                stream,
                _catalogSerializerOptions
            );
            if (catalog is null || catalog.Imports.Count == 0)
            {
                throw new InvalidDataException("The glossary import catalog is empty or invalid.");
            }

            if (
                catalog.Imports.GroupBy(item => item.Asset, StringComparer.Ordinal).Any(group =>
                    group.Count() > 1
                )
            )
            {
                throw new InvalidDataException(
                    "The glossary import catalog contains duplicate assets."
                );
            }

            return catalog;
        }

        internal static IReadOnlyList<GlossaryTermSubmission> LoadSubmissions(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes, writable: false);
            using var reader = new StreamReader(stream);
            var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                MissingFieldFound = null,
            };
            using var csv = new CsvReader(reader, configuration);
            csv.Context.TypeConverterCache.AddConverter<bool>(new BooleanConverter());
            csv.Context.RegisterClassMap(new GlossaryMapper());
            csv.Read();
            csv.ReadHeader();

            var submissions = new List<GlossaryTermSubmission>();
            var recordNumber = 0;
            while (csv.Read())
            {
                recordNumber++;
                try
                {
                    var term = csv.GetRecord<GlossaryModel>();
                    term.SourceRecordNumber = recordNumber;
                    submissions.Add(
                        new GlossaryTermSubmission(
                            recordNumber,
                            term,
                            term.Name,
                            term.StaticId,
                            csv.Parser.RawRecord,
                            term.BreakingChange,
                            [],
                            true
                        )
                    );
                }
                catch (Exception exception)
                    when (exception is CsvHelperException or FormatException)
                {
                    submissions.Add(
                        new GlossaryTermSubmission(
                            recordNumber,
                            null,
                            GetField(csv, "Name"),
                            GetField(csv, "StaticId"),
                            csv.Parser.RawRecord,
                            false,
                            [$"The submitted record could not be read: {exception.Message}"],
                            false
                        )
                    );
                }
            }

            return submissions;
        }

        private static string GetField(CsvReader csv, string name)
        {
            try
            {
                return csv.GetField(name) ?? "";
            }
            catch (CsvHelperException)
            {
                return "";
            }
        }

        private static byte[] ReadAsset(string assetName)
        {
            using var stream = OpenAsset(assetName);
            using var memory = new MemoryStream();
            stream.CopyTo(memory);
            return memory.ToArray();
        }

        private static Stream OpenAsset(string assetName)
        {
            var resourceName = $"Semantics.Assets.{assetName}";
            var assembly = Assembly.GetAssembly(typeof(GlossaryImportService));
            return assembly!.GetManifestResourceStream(resourceName)
                ?? throw new FileNotFoundException($"File {resourceName} not found.");
        }
    }
}
