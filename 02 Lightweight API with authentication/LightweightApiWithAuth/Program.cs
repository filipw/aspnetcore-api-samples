using System;
using System.IO;
using LightweightApiWithAuth.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using IdentityServer4.AccessTokenValidation;

namespace LightweightApiWithAuth
{
    public class Program
    {
        public static void Main(string[] args) =>
            new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables();
                })
                .ConfigureLogging((hostingContext, l) =>
                {
                    l.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
                    l.AddConsole();
                })
                .UseIISIntegration()
                .ConfigureServices(s =>
                {
                    // set up embedded identity server
                    s.AddIdentityServer().
                        AddTestClients().
                        AddTestResources().
                        AddDeveloperSigningCredential();

                    s.AddRouting()
                    .AddAuthorization(options =>
                    {
                        // set up authorization policy for the API
                        options.AddPolicy("API", policy =>
                        {
                            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                            policy.RequireAuthenticatedUser().RequireClaim("scope", "read");
                        });
                    })
                    .AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                    .AddIdentityServerAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme, o =>
                    {
                        o.Authority = "http://localhost:5000/openid";
                        o.RequireHttpsMetadata = false;
                    });
                })
                .Configure(app =>
                {
                    app.Map("/openid", id =>
                    {
                        // use embedded identity server to issue tokens
                        id.UseIdentityServer();
                    })
                    .UseAuthentication() // consume the JWT tokens in the API
                    .Use(async (c, next) => // authorize the whole API against the API policy
                     {
                         var allowed = await c.RequestServices.GetRequiredService<IAuthorizationService>().AuthorizeAsync(c.User, null, "API");
                         if (allowed.Succeeded) await next();
                         else
                             c.Response.StatusCode = 401;
                     })
                    .UseRouter(r => // define all API endpoints
                    {
                        var contactRepo = new InMemoryContactRepository();

                        r.MapGet("contacts", async (request, response, routeData) =>
                        {
                            var contacts = await contactRepo.GetAll();
                            response.WriteJson(contacts);
                        });

                        r.MapGet("contacts/{id:int}", async (request, response, routeData) =>
                        {
                            var contact = await contactRepo.Get(Convert.ToInt32(routeData.Values["id"]));
                            if (contact == null)
                            {
                                response.StatusCode = 404;
                                return;
                            }

                            response.WriteJson(contact);
                        });
                    });
                })
                .Build().Run();
    }
}