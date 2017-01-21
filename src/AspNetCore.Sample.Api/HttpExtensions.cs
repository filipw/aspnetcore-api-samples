using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace AspNetCore.Sample.Api
{
    public static class HttpExtensions
    {
        public static Task WriteJson<T>(this HttpResponse response, T obj)
        {
            response.ContentType = "application/json";
            return response.WriteAsync(JsonConvert.SerializeObject(obj));
        }

        public static async Task<T> ReadFromJson<T>(this HttpContext httpContext)
        {
            var serializer = new JsonSerializer();

            using (var sr = new StreamReader(httpContext.Request.Body))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                var obj = serializer.Deserialize<T>(jsonTextReader);

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
}