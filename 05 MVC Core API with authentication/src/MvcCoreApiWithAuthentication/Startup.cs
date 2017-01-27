using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MvcCoreApiWithAuthentication.Models;

namespace MvcCoreApiWithAuthentication
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
            var readPolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).
                RequireAuthenticatedUser().
                RequireClaim("scope", "read").Build();

            var writePolicy = new AuthorizationPolicyBuilder(JwtBearerDefaults.AuthenticationScheme).
                RequireAuthenticatedUser().
                RequireClaim("scope", "write").Build();

            services.AddSingleton<IContactRepository, InMemoryContactRepository>();
            services.AddMvcCore(opt =>
                {
                    opt.Filters.Add(new AuthorizeFilter("ReadPolicy"));
                }).AddAuthorization(o =>
                {
                    o.AddPolicy("ReadPolicy", readPolicy);
                    o.AddPolicy("WritePolicy", writePolicy);
                }).AddDataAnnotations().
                AddJsonFormatters();

            // set up embedded identity server
            services.AddIdentityServer().
                AddTestClients().
                AddTestResources().
                AddTemporarySigningCredential();
        }

        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            app.Map("/openid", id => {
                // use embedded identity server to issue tokens
                id.UseIdentityServer();
            });

            app.Map("/api", api => {
                // consume the JWT tokens in the API
                api.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
                {
                    Authority = "http://localhost:5000/openid",
                    RequireHttpsMetadata = false,
                });

                api.UseMvc();
            });
        }
    }
}
