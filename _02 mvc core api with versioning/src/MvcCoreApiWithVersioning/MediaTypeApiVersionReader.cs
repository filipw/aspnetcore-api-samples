using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc.Versioning;

namespace MvcCoreApi
{
    public class MediaTypeApiVersionReader : IApiVersionReader
    {
        private readonly string[] _mediaTypes = { "application/vnd.demo" };

        public string Read(HttpRequest request)
        {
            var headers = new RequestHeaders(request.Headers);

            var acceptHeaderVersion = headers.Accept.FirstOrDefault(x => _mediaTypes.Any(a => x.MediaType.ToLowerInvariant().Contains(a)));

            if (acceptHeaderVersion != null && acceptHeaderVersion.MediaType.Contains("-v") &&
                acceptHeaderVersion.MediaType.Contains("+"))
            {
                return acceptHeaderVersion.MediaType.Between("-v", "+");
            }

            return null;
        }
    }
}