using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Linq;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace MvcCoreApiWithDocs
{
    public class ScopesDefinitionOperationFilter : IOperationFilter 
    {
        private readonly Dictionary<string, string> _policyToScopesMappings;

        public ScopesDefinitionOperationFilter(Dictionary<string, string> policyToScopesMappings)
        {
            _policyToScopesMappings = policyToScopesMappings;
        }

        public void Apply(Operation operation, OperationFilterContext context)
        {
            var requiredScopes = new HashSet<string>();

            var controllerActionDescriptor = context.ApiDescription.GetProperty<ControllerActionDescriptor>();
            var globalPolicies = controllerActionDescriptor.FilterDescriptors.Where(x => x.Scope == FilterScope.Global).
                Select(x => x.Filter).
                OfType<AuthorizeFilter>().
                Select(x => x.AuthorizeData.FirstOrDefault().Policy);

            var controllerPolicies = context.ApiDescription.ControllerAttributes()
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.Policy);

            var actionPolicies = context.ApiDescription.ActionAttributes()
                .OfType<AuthorizeAttribute>()
                .Select(attr => attr.Policy);

            var allPolicies = globalPolicies.Union(actionPolicies.Union(controllerPolicies)).Distinct();

            foreach (var policy in allPolicies)
            {
                if (_policyToScopesMappings.ContainsKey(policy))
                {
                    var policyScope = _policyToScopesMappings[policy];
                    requiredScopes.Add(policyScope);
                }
            }

            if (requiredScopes.Any())
            {
                operation.Responses.Add("401", new Response { Description = "Unauthorized" });
                operation.Responses.Add("403", new Response { Description = "Forbidden" });

                operation.Security = new List<IDictionary<string, IEnumerable<string>>>
                {
                    new Dictionary<string, IEnumerable<string>>
                    {
                        {"oauth2", requiredScopes}
                    }
                };
            }
        }
    }
}