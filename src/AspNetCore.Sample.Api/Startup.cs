using System;
using AspNetCore.Sample.Api.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspNetCore.Sample.Api
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
            services.AddSingleton<IContactRepository, InMemoryContactRepository>();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            var contactRepo = app.ApplicationServices.GetRequiredService<IContactRepository>();

            app.UseRouter(r =>
            {
                r.MapGet("contacts", async (request, response, routeData) =>
                {
                    var contacts = contactRepo.GetAll();
                    await response.WriteJson(contacts);
                });

                r.MapGet("contacts/{id:int}", async (request, response, routeData) =>
                {
                    var contact = await contactRepo.Get(Convert.ToInt32(routeData.Values["id"]));
                    if (contact == null)
                    {
                        
                        response.StatusCode = 404;
                        return;
                    }

                    await response.WriteJson(contact);
                });

                r.MapPost("contacts", async (request, response, routeData) =>
                {
                    var newContact = await request.HttpContext.ReadFromJson<Contact>();
                    if (newContact == null) return;

                    await contactRepo.Add(newContact);

                    response.StatusCode = 201;
                    await response.WriteJson(newContact);
                });

                r.MapPut("contacts/{id:int}", async (request, response, routeData) =>
                {
                    var updatedContact = await request.HttpContext.ReadFromJson<Contact>();
                    if (updatedContact == null) return;

                    updatedContact.ContactId = Convert.ToInt32(routeData.Values["id"]);
                    await contactRepo.Update(updatedContact);

                    response.StatusCode = 204;
                });

                r.MapDelete("contacts/{id:int}", async (request, response, routeData) =>
                {
                    await contactRepo.Delete(Convert.ToInt32(routeData.Values["id"]));
                    response.StatusCode = 204;
                });
            });

            app.UseMvc();
        }
    }
}
