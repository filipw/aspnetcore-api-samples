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

namespace LightweightApiWithAuth
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddEnvironmentVariables().Build();

            var host = new WebHostBuilder()
                .UseKestrel()
                .UseConfiguration(config)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .ConfigureLogging(l => l.AddConsole(config.GetSection("Logging")))
                .ConfigureServices(s =>
                {
                    // set up embedded identity server
                    s.AddIdentityServer().
                        AddTestClients().
                        AddTestResources().
                        AddTemporarySigningCredential();

                    s.AddRouting();

                    // set up authorization policy for the API
                    s.AddAuthorization(options =>
                    {
                        options.AddPolicy("API", policy =>
                        {
                            policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                            policy.RequireAuthenticatedUser().RequireClaim("scope", "read");
                        });
                    });
                })
                .Configure(app =>
                {
                    // use embedded identity server to issue tokens
                    app.UseIdentityServer();

                    // consume the JWT tokens in the API
                    app.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
                    {
                        Authority = "http://localhost:34917",
                        RequireHttpsMetadata = false,
                    });

                    // authorize the whole API against the API policy
                    app.Use(async (c, next) =>
                    {
                        var authz = c.RequestServices.GetRequiredService<IAuthorizationService>();
                        var allowed = await authz.AuthorizeAsync(c.User, null, "API");
                        if (allowed)
                        {
                            await next();
                        }
                        else
                        {
                            c.Response.StatusCode = 401;
                        }
                    });

                    // define all API endpoints
                    app.UseRouter(r =>
                    {
                        var contactRepo = new InMemoryContactRepository();

                        r.MapGet("contacts", async (request, response, routeData) =>
                        {
                            var contacts = await contactRepo.GetAll();
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
                    });
                })
                .Build();

            host.Run();
        }
    }
}
