namespace Adr.Semantics
{
    using System.Diagnostics.CodeAnalysis;
    using Adr.Semantics.Configuration;
    using Adr.Semantics.Data;
    using Adr.Semantics.Providers;
    using Adr.Semantics.Services;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Configures the application during startup.
    /// </summary>
    public class Startup
    {
        private readonly StartupConfiguration startupConfig;
        private readonly IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="env">The injected Environment provider.</param>
        /// <param name="configuration">The injected configuration provider.</param>
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            this.configuration = configuration;
            this.startupConfig = new StartupConfiguration(configuration, env);
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The injected services provider.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            this.startupConfig.ConfigureForwardHeaders(services);
            this.startupConfig.ConfigureHttpServices(services);
            this.startupConfig.ConfigureSwaggerServices(services);
            this.startupConfig.ConfigureTracing(services);

            // Configure the semantic services
            services.AddTransient<IGlossaryService, GlossaryService>();
            services.AddDbContextFactory<GlossaryDbContext>(options =>
                options.UseSqlite(
                    this.configuration.GetConnectionString("Semantics")
                        ?? "Data Source=semantics.db"
                )
            );
            services.AddSingleton<GlossaryReleasePublisher>();
            services.AddSingleton<GlossaryTermProcessor>();
            services.AddSingleton<GlossaryImportService>();
            services.AddSingleton<IGlossaryProvider, GlossaryDatabaseProvider>();
            services.AddTransient<IGlossaryEditingService, GlossaryEditingService>();
            services.AddMemoryCache();
            services.AddTransient<IDictionaryService, DictionaryService>();
            services.AddHttpClient();
            services.AddSingleton<IDictionaryProvider, OpenApiProvider>();

            services.AddCors(options =>
            {
                options.AddPolicy(
                    "allowAny",
                    policy =>
                    {
                        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
                    }
                );
            });
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        public void Configure(IApplicationBuilder app)
        {
            // Apply database updates and import ordered glossary sources before serving requests.
            app.ApplicationServices.GetRequiredService<GlossaryImportService>().Initialize();
            _ = app.ApplicationServices.GetRequiredService<IGlossaryProvider>();

            this.startupConfig.UseForwardHeaders(app);
            this.startupConfig.UseHttp(app);
            this.startupConfig.UseResponseCaching(app);
            //this.startupConfig.UseAuth(app); not yet
            this.startupConfig.UseEnrichTracing(app);
            this.startupConfig.UseRest(app);
            this.startupConfig.UseSwagger(app);
        }
    }
}
