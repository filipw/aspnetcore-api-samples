using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace MvcCoreApi
{
    public class MediaTypeApiVersionReader : IApiVersionReader
    {
        private readonly string[] _mediaTypes = { "application/vnd.demo" };

        public string Read(HttpRequest request)
        {
            var headers = request.GetTypedHeaders();

            var acceptHeaderVersion = headers.Accept.FirstOrDefault(x => _mediaTypes.Any(a => x.MediaType.ToString().ToLowerInvariant().Contains(a)));

            if (acceptHeaderVersion != null && acceptHeaderVersion.MediaType.ToString().Contains("-v") &&
                acceptHeaderVersion.MediaType.ToString().Contains("+"))
            {
                return acceptHeaderVersion.MediaType.ToString().Between("-v", "+");
            }

            return null;
        }
    }
}