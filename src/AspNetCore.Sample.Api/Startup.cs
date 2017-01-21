using System;
using AspNetCore.Sample.Api.Models;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
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
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<IContactRepository, InMemoryContactRepository>();
            services.AddRouting();

            // set up embedded identity server
            services.AddIdentityServer().AddInMemoryClients(new[]
            {
                new Client
                {
                    ClientId = "client1",
                    ClientSecrets =
                    {
                        new Secret("secret1".Sha256())
                    },
                    AllowedGrantTypes = new[]
                    {
                        GrantType.ClientCredentials
                    },
                    AllowedScopes = new[]
                    {
                        "testscope"
                    }
                }
            }).AddInMemoryApiResources(new[]
            {
                new ApiResource("embedded")
                {
                    Scopes =
                    {
                        new Scope("testscope")
                    },
                    Enabled = true
                },
            }).AddTemporarySigningCredential();

            // set up authorization policy for the API
            services.AddAuthorization(options =>
            {
                options.AddPolicy("API", policy =>
                {
                    policy.AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme);
                    policy.RequireAuthenticatedUser()
                          .RequireClaim("scope", "testscope");
                });
            });
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            var contactRepo = app.ApplicationServices.GetRequiredService<IContactRepository>();

            // use embedded identity server to issue tokens
            app.UseIdentityServer();

            // consume the JWT tokens in the API
            app.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
            {
                Authority = "http://localhost:28134",
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
        }
    }
}
