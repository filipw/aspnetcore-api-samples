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
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace LightweightApi
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
                            response.WriteJson(contacts);
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

        public static void WriteJson<T>(this HttpResponse response, T obj)
        {
            response.ContentType = "application/json";
            using (var writer = new HttpResponseStreamWriter(response.Body, Encoding.UTF8))
            {
                using (var jsonWriter = new JsonTextWriter(writer))
                {
                    jsonWriter.CloseOutput = false;
                    jsonWriter.AutoCompleteOnClose = false;

                    Serializer.Serialize(jsonWriter, obj);
                }
            }
        }

        public static T ReadFromJson<T>(this HttpContext httpContext)
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
                httpContext.Response.WriteJson(results);

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
