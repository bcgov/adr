namespace Adr.PublicBodies
{
    using System.Diagnostics.CodeAnalysis;
    using Adr.PublicBodies.Configuration;
    using Adr.PublicBodies.Configuration.Models;
    using Adr.PublicBodies.Providers;
    using Adr.PublicBodies.Services;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    /// <summary>
    /// Configures the application during startup.
    /// </summary>
    public class Startup
    {
        private readonly StartupConfiguration _startupConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="env">The injected Environment provider.</param>
        /// <param name="configuration">The injected configuration provider.</param>
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            _startupConfig = new StartupConfiguration(configuration, env);
        }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services">The injected services provider.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            _startupConfig.ConfigureForwardHeaders(services);
            _startupConfig.ConfigureHttpServices(services);
            _startupConfig.ConfigureSwaggerServices(services);
            _startupConfig.ConfigureTracing(services);

            services.Configure<ChefsConfiguration>(
                _startupConfig.Configuration.GetSection("Chefs")
            );
            services.AddTransient<IChefsTokenService, ChefsTokenService>();

            // Configure the public bodies services
            services.AddTransient<IPublicBodyService, PublicBodyService>();
            services.AddSingleton<IPublicBodyProvider, StaticFileProvider>();

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
            _startupConfig.UseForwardHeaders(app);
            _startupConfig.UseHttp(app);
            _startupConfig.UseResponseCaching(app);
            //_startupConfig.UseAuth(app); not yet
            _startupConfig.UseEnrichTracing(app);
            _startupConfig.UseRest(app);
            _startupConfig.UseSwagger(app);
        }
    }
}
