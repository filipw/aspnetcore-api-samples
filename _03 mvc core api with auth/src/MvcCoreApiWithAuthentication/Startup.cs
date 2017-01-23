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
            var authorizationPolicy = new AuthorizationPolicyBuilder().
                AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme).
                RequireAuthenticatedUser().RequireClaim("scope", "testscope").Build();

            services.AddSingleton<IContactRepository, InMemoryContactRepository>();
            services.AddMvcCore(opt =>
                {
                    opt.Filters.Add(new AuthorizeFilter(authorizationPolicy));
                }).AddAuthorization(o =>
                {
                    o.AddPolicy("API", authorizationPolicy);
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

            // use embedded identity server to issue tokens
            app.UseIdentityServer();

            // consume the JWT tokens in the API
            app.UseIdentityServerAuthentication(new IdentityServerAuthenticationOptions
            {
                Authority = "http://localhost:28134",
                RequireHttpsMetadata = false,
            });

            app.UseMvc();
        }
    }
}
