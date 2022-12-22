// Accenture.MyWizard.Ingrain.UI.DotNetWrapper.Startup
using System;
using System.Threading.Tasks;
using Accenture.MyWizard.Cryptography;
using Accenture.MyWizard.Fortress.AuthProviders.Extensions;
using Accenture.MyWizard.Ingrain.UI.DotNetWrapper;
using Accenture.MyWizard.LOGGING;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

public class Startup
{
	public IConfiguration Configuration { get; }

	public IWebHostEnvironment HostingEnvironment { get; }

	public Startup(IConfiguration configuration, IWebHostEnvironment env)
	{
		Configuration = configuration;
		HostingEnvironment = env;
	}

	public void ConfigureServices(IServiceCollection services)
	{
		try
		{
			LogManager.SetLogFolderPath(string.Empty);
			services.AddControllers().AddNewtonsoftJson(delegate (MvcNewtonsoftJsonOptions options)
			{
				options.SerializerSettings.Formatting = Formatting.Indented;
				options.SerializerSettings.ContractResolver = new DefaultContractResolver();
			});
			services.ConfigureCryptography();
			//services.ConfigureFortress("authprovider");
			services.AddHttpContextAccessor();
			//services.AddHealthChecks().AddCheck("Test health check", () => HealthCheckResult.Healthy("Server is Healthy"));
			services.AddMvcCore(delegate (MvcOptions options)
			{
				options.EnableEndpointRouting = false;
			}).AddRazorViewEngine();
		}
		catch (Exception ex)
		{
			LogManager.Logger.LogErrorMessage(typeof(Startup), "ConfigureServices", ex.Message, ex, string.Empty, string.Empty, string.Empty, string.Empty);
		}
	}

	public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
	{
		LogManager.Logger.LogProcessInfo(typeof(Startup), "Configure", "UI Startup.cs Configure Start", string.Empty, string.Empty, string.Empty, string.Empty);
		string value = Program.AppSettingsJson.GetAppSettings().GetSection("AppSettings").GetSection("applicationSuffix")
			.Value;
		if (!string.IsNullOrEmpty(value))
		{
			app.UsePathBase(value);
			LogManager.Logger.LogProcessInfo(typeof(Startup), "Configure", "WebClient-StartUp execution - Application   Suffix = " + value, string.Empty, string.Empty, string.Empty, string.Empty);
		}
		else
		{
			LogManager.Logger.LogProcessInfo(typeof(Startup), "Configure", "WebClient-StartUp execution - Application Suffix not defined for this application", string.Empty, string.Empty, string.Empty, string.Empty);
		}
		if (env.IsDevelopment())
		{
			app.Use(delegate (HttpContext context, Func<Task> next)
			{
				context.Request.Scheme = "https";
				return next();
			});
			app.UseDeveloperExceptionPage();
		}
		else
		{
			app.Use(delegate (HttpContext context, Func<Task> next)
			{
				context.Request.Scheme = "https";
				return next();
			});
			app.UseExceptionHandler("/Home/Error");
		}
		app.UseStatusCodePagesWithReExecute("/");
		app.UseRouting();
		app.UseHttpsRedirection();
		app.UseDefaultFiles();
		app.UseStaticFiles();
		app.UseAuthentication();
		app.UseAuthorization();
		app.UseEndpoints(delegate (IEndpointRouteBuilder endpoints)
		{
			endpoints.MapControllerRoute("default", "{controller=landingPage}/{action=Index}/{id?}");
		});
		LogManager.Logger.LogProcessInfo(typeof(Startup), "Configure", "UI Startup.cs Configure End", string.Empty, string.Empty, string.Empty, string.Empty);
		//endpoints.MapHealthChecks("healthcheck");
	}
}
