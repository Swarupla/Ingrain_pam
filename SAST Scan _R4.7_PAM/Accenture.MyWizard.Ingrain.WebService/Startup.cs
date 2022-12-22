using Accenture.MyWizard.Cryptography;
using Accenture.MyWizard.Fortress.AuthProviders.Extensions;
using Accenture.MyWizard.Shared.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;

namespace Accenture.MyWizard.Ingrain.WebService
{
    public class Startup
    {
        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            HostingEnvironment = env;
        }

        /// <summary>
        /// Configuration
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// HostingEnvironment
        /// </summary>
        public IWebHostEnvironment HostingEnvironment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            LOGGING.LogManager.SetLogFolderPath(string.Empty);

            try
            {
                var assembly = typeof(Module).GetTypeInfo().Assembly;

                services.AddControllers()
                    .AddNewtonsoftJson(options =>
                    {
                        options.SerializerSettings.Formatting = Newtonsoft.Json.Formatting.Indented;
                        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                    })
                     // .SetCompatibilityVersion(CompatibilityVersion.Version_2_2)
                     .AddControllersAsServices()
                    .ConfigureApiBehaviorOptions(options =>
                    {
                        options.SuppressModelStateInvalidFilter = true;
                    })
                    .AddApplicationPart(assembly);

                services.Configure<IngrainAppSettings>(Configuration.GetSection("AppSettings"));
                services.Configure<CookiePolicyOptions>(options =>
                {
                    // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                    options.CheckConsentNeeded = context => true;
                    options.MinimumSameSitePolicy = SameSiteMode.None;
                });

                // Added to handle large file upload by Shreya
                services.Configure<FormOptions>(options =>
                {
                    options.MultipartBodyLengthLimit = int.MaxValue;
                });


                //Start - This section will enable cryptography
                //services.ConfigureCryptography();
                //services.ConfigureFortress("authprovider");  
                services.AddHttpContextAccessor();
                services.AddHealthChecks().AddCheck("Test health check", () => HealthCheckResult.Healthy("Server is Healthy"));
                //End

                ///Enabling CORS

                var authProvider = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("authProvider").Value;

                if (authProvider == "WindowsAuthProvider")
                {
                    services.AddCors(options =>
                    {
                        options.AddPolicy("AllowAllOrigins",
                            builder =>
                            {
                                // builder.AllowAnyOrigin()
                                builder.WithOrigins(Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("origin").Value)
                                    .SetIsOriginAllowedToAllowWildcardSubdomains()
                                    .AllowCredentials()
                                    .AllowAnyHeader()
                                    .AllowAnyMethod();
                            });
                    });
                } else
                {
                   services.AddCors(options =>
                {
                    options.AddPolicy("AllowAllOrigins",
                        builder =>
                        {
                            // builder.AllowAnyOrigin()
                            builder.WithOrigins(Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("origin").Value)
                            .AllowAnyHeader()
                            .AllowAnyMethod();
                        });
                });
                }

                ////added to load dynamic assemblies
                Utility.LoadAssemblyDependencies(services);

            }
            catch (Exception ex)
            {
                LOGGING.LogManager.Logger.LogErrorMessage(typeof(Startup), nameof(ConfigureServices), ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);

            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            LOGGING.LogManager.SetLogFolderPath(string.Empty);
            var applicationSuffix = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("applicationSuffix").Value;

            //Get the ApplicationSuffix if exists
            if (!String.IsNullOrEmpty(applicationSuffix))
            {
                app.UsePathBase(applicationSuffix);
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Startup), nameof(Configure), "WebService-StartUp execution - Application   Suffix = " + applicationSuffix, string.Empty, string.Empty, string.Empty, string.Empty);
            }
            else
            {
                LOGGING.LogManager.Logger.LogProcessInfo(typeof(Startup), nameof(Configure), "WebService-StartUp execution - Application Suffix not defined for this application", string.Empty, string.Empty, string.Empty, string.Empty);
            }
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();
            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseCors("AllowAllOrigins");

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapHealthChecks("healthcheck");
            });

        }
    }
}
