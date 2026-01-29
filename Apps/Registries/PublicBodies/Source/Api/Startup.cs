namespace Adr.PublicBodies
{
    using System.Diagnostics.CodeAnalysis;
    using Adr.PublicBodies.Configuration;
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
        private readonly StartupConfiguration startupConfig;

        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="env">The injected Environment provider.</param>
        /// <param name="configuration">The injected configuration provider.</param>
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
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

            // Configure the public bodies services
            services.AddTransient<IPublicBodyService, PublicBodyService>();
            services.AddSingleton<IPublicBodyProvider, StaticFileProvider>();

            // Add auto mapper
            services.AddAutoMapper(typeof(Startup));

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
            this.startupConfig.UseForwardHeaders(app);
            this.startupConfig.UseSwagger(app);
            this.startupConfig.UseHttp(app);
            this.startupConfig.UseResponseCaching(app);
            //this.startupConfig.UseAuth(app); not yet
            this.startupConfig.UseEnrichTracing(app);
            this.startupConfig.UseRest(app);
        }
    }
}
