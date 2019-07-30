namespace TestHealthCheck
{
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.DependencyInjection.Extensions;
    using Microsoft.Extensions.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Hosting;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        private const string HealthCheckServiceAssembly = "Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckPublisherHostedService";

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            _ = services.AddHostedService<StartupHostedService>();
            _ = services.AddSingleton<StartupHostedServiceHealthCheck>();

            _ = services
             .AddHealthChecks()
             .AddCheck<ExampleHealthCheck>("example_health_check")
             .AddMemoryHealthCheck("memory")
             .AddCheck("Foo", () => HealthCheckResult.Healthy("Foo is OK!"), tags: new[] { "foo_tag" })
             .AddCheck("Bar", () => HealthCheckResult.Unhealthy("Bar is unhealthy!"), tags: new[] { "bar_tag" })
             .AddCheck("Baz", () => HealthCheckResult.Healthy("Baz is OK!"), tags: new[] { "baz_tag" })
             .AddCheck<StartupHostedServiceHealthCheck>("hosted_service_startup", failureStatus: HealthStatus.Degraded, tags: new[] { "ready" });

            _ = services.Configure<HealthCheckPublisherOptions>(options =>
              {
                  options.Delay = TimeSpan.FromSeconds(2);
                  options.Predicate = (check) => true;// check.Tags.Contains("ready");
              });

            services.TryAddEnumerable(ServiceDescriptor.Singleton(typeof(IHostedService), typeof(HealthCheckPublisherOptions).Assembly.GetType(HealthCheckServiceAssembly)));
            _ = services.AddSingleton<IHealthCheckPublisher, ReadinessPublisher>();

            _ = services.AddApplicationInsightsTelemetry();
            _ = services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_2);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, Microsoft.AspNetCore.Hosting.IHostingEnvironment env)
        {
            _ = app.UseHealthChecks("/health", new HealthCheckOptions()
            {
                // WriteResponse is a delegate used to write the response.
                ResponseWriter = WriteResponse
            });

            if (env.IsDevelopment())
            {
                _ = app.UseDeveloperExceptionPage();
            }

            _ = app.UseMvc();
        }

        private static Task WriteResponse(HttpContext httpContext, HealthReport result)
        {
            httpContext.Response.ContentType = "application/json";

            var json = new JObject(
                new JProperty("status", result.Status.ToString()),
                new JProperty("results", new JObject(result.Entries.Select(pair =>
                    new JProperty(pair.Key, new JObject(
                        new JProperty("status", pair.Value.Status.ToString()),
                        new JProperty("description", pair.Value.Description),
                        new JProperty("data", new JObject(pair.Value.Data.Select(
                            p => new JProperty(p.Key, p.Value))))))))));

            return httpContext.Response.WriteAsync(json.ToString(Formatting.Indented));
        }
    }
}
