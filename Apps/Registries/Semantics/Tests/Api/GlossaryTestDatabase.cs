namespace Adr.Semantics.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Adr.Semantics.Data;
    using Adr.Semantics.Providers;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Mvc.Testing;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Logging.Abstractions;

    internal sealed class GlossaryTestDatabase : IDisposable
    {
        private readonly string _databasePath;

        public GlossaryTestDatabase(
            ILogger<GlossaryImportService>? logger = null,
            ILogger<GlossaryTermProcessor>? termLogger = null
        )
        {
            _databasePath = Path.Combine(
                Path.GetTempPath(),
                $"adr-semantics-tests-{Guid.NewGuid():N}.db"
            );
            Factory = new TestDbContextFactory(
                new DbContextOptionsBuilder<GlossaryDbContext>()
                    .UseSqlite($"Data Source={_databasePath}")
                    .Options
            );
            Processor = new GlossaryTermProcessor(
                termLogger ?? NullLogger<GlossaryTermProcessor>.Instance
            );
            Publisher = new GlossaryReleasePublisher(
                NullLogger<GlossaryReleasePublisher>.Instance
            );
            Importer = new GlossaryImportService(
                Factory,
                logger ?? NullLogger<GlossaryImportService>.Instance,
                Publisher,
                Processor
            );
            Importer.Initialize();
            Provider = new GlossaryDatabaseProvider(Factory);
        }

        public TestDbContextFactory Factory { get; }

        public GlossaryImportService Importer { get; }

        public GlossaryTermProcessor Processor { get; }

        public GlossaryReleasePublisher Publisher { get; }

        public GlossaryDatabaseProvider Provider { get; }

        public void Dispose()
        {
            File.Delete(_databasePath);
            File.Delete($"{_databasePath}-shm");
            File.Delete($"{_databasePath}-wal");
        }
    }

    internal sealed class TestDbContextFactory : IDbContextFactory<GlossaryDbContext>
    {
        private readonly DbContextOptions<GlossaryDbContext> _options;

        public TestDbContextFactory(DbContextOptions<GlossaryDbContext> options)
        {
            _options = options;
        }

        public GlossaryDbContext CreateDbContext()
        {
            return new GlossaryDbContext(_options);
        }
    }

    public sealed class GlossaryApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _databasePath = Path.Combine(
            Path.GetTempPath(),
            $"adr-semantics-api-tests-{Guid.NewGuid():N}.db"
        );

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration(
                (_, configuration) =>
                    configuration.AddInMemoryCollection(
                        new Dictionary<string, string?>
                        {
                            ["ConnectionStrings:Semantics"] = $"Data Source={_databasePath}",
                        }
                    )
            );
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            File.Delete(_databasePath);
            File.Delete($"{_databasePath}-shm");
            File.Delete($"{_databasePath}-wal");
        }
    }
}
