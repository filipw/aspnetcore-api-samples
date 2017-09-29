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
        public static void Main(string[] args)
        {
            var host = new WebHostBuilder()
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

                    s.AddRouting();

                    s.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                        .AddIdentityServerAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme, o =>
                        {
                            o.Authority = "http://localhost:5000/openid";
                            o.RequireHttpsMetadata = false;
                        });

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
                    app.Map("/openid", id => {
                        // use embedded identity server to issue tokens
                        id.UseIdentityServer();
                    });

                    // consume the JWT tokens in the API
                    app.UseAuthentication();

                    // authorize the whole API against the API policy
                    app.Use(async (c, next) =>
                    {
                        var authz = c.RequestServices.GetRequiredService<IAuthorizationService>();
                        var allowed = await authz.AuthorizeAsync(c.User, null, "API");
                        if (allowed.Succeeded)
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
                .Build();

            host.Run();
        }
    }
}
