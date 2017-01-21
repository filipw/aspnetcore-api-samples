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
            return response.WriteAsync(JsonConvert.SerializeObject(obj));
        }

        public static T ReadFromJson<T>(this HttpRequest response)
        {
            var serializer = new JsonSerializer();

            using (var sr = new StreamReader(response.Body))
            using (var jsonTextReader = new JsonTextReader(sr))
            {
                return serializer.Deserialize<T>(jsonTextReader);
            }
        }
    }
}