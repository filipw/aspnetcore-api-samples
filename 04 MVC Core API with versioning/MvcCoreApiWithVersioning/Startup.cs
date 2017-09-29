using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MvcCoreApi.Models;

namespace MvcCoreApi
{
    public class Startup
    {
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IContactRepository, InMemoryContactRepository>();
            services.AddMvcCore().
                AddDataAnnotations().
                AddJsonFormatters();

            services.AddApiVersioning(o =>
            {
                o.DefaultApiVersion = ApiVersion.Parse("1");
                o.AssumeDefaultVersionWhenUnspecified = true;
                o.ReportApiVersions = true;
                // o.ApiVersionReader = new HeaderApiVersionReader("version");

                // request "application/vnd.demo-v2+json"
                o.ApiVersionReader = new MediaTypeApiVersionReader();
            });
        }

        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseMvc();
        }
    }
}
