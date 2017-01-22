using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LightweightApi
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
                .ConfigureServices(s => s.AddRouting())
                .Configure(app =>
                {
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
                })
                .Build();

            host.Run();
        }
    }

    public static class HttpExtensions
    {
        private static readonly JsonSerializer Serializer = new JsonSerializer();

        public static Task WriteJson<T>(this HttpResponse response, T obj)
        {
            response.ContentType = "application/json";
            return response.WriteAsync(JsonConvert.SerializeObject(obj));
        }

        public static async Task<T> ReadFromJson<T>(this HttpContext httpContext)
        {
            using (var streamReader = new StreamReader(httpContext.Request.Body))
            using (var jsonTextReader = new JsonTextReader(streamReader))
            {
                var obj = Serializer.Deserialize<T>(jsonTextReader);

                var results = new List<ValidationResult>();
                if (Validator.TryValidateObject(obj, new ValidationContext(obj), results))
                {
                    return obj;
                }

                httpContext.Response.StatusCode = 400;
                await httpContext.Response.WriteJson(results);

                return default(T);
            }
        }
    }

    public class Contact
    {
        public int ContactId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Address { get; set; }

        public string City { get; set; }
    }

    public class InMemoryContactRepository
    {
        private readonly List<Contact> _contacts = new List<Contact>
        {
            new Contact { ContactId = 1, Name = "Filip W", Address = "Bahnhofstrasse 1", City = "Zurich" },
            new Contact { ContactId = 2, Name = "Josh Donaldson", Address = "1 Blue Jays Way", City = "Toronto" }, 
            new Contact { ContactId = 3, Name = "Aaron Sanchez", Address = "1 Blue Jays Way", City = "Toronto" },
            new Contact { ContactId = 4, Name = "Jose Bautista", Address = "1 Blue Jays Way", City = "Toronto" },
            new Contact { ContactId = 5, Name = "Edwin Encarnacion", Address = "1 Blue Jays Way", City = "Toronto" }        
        };

        public Task<IEnumerable<Contact>> GetAll()
        {
            return Task.FromResult(_contacts.AsEnumerable());
        }

        public Task<Contact> Get(int id)
        {
            return Task.FromResult(_contacts.FirstOrDefault(x => x.ContactId == id));
        }

        public Task<int> Add(Contact contact)
        {
            var newId = (_contacts.LastOrDefault()?.ContactId ?? 0) + 1;
            contact.ContactId = newId;
            _contacts.Add(contact);
            return Task.FromResult(newId);
        }

        public async Task Update(Contact updatedContact)
        {
            var contact = await Get(updatedContact.ContactId).ConfigureAwait(false);
            if (contact == null)
            {
                throw new InvalidOperationException(string.Format("Contact with id '{0}' does not exists", updatedContact.ContactId));
            }

            contact.Address = updatedContact.Address;
            contact.City = updatedContact.City;
            contact.Name = updatedContact.Name;
        }

        public async Task Delete(int id)
        {
            var contact = await Get(id).ConfigureAwait(false);
            if (contact == null)
            {
                throw new InvalidOperationException(string.Format("Contact with id '{0}' does not exists", id));
            }

            _contacts.Remove(contact);
        }
    }
}
