namespace Adr.Semantics.Configuration.Addons.Swagger
{
    using System.Collections.Generic;
    using System.Linq;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;

    /// <summary>
    /// Explicitly documents anonymous operations in the generated OpenAPI contract.
    /// </summary>
    public sealed class AllowAnonymousOperationFilter : IOperationFilter
    {
        /// <inheritdoc/>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (
                context.ApiDescription.ActionDescriptor.EndpointMetadata
                    .OfType<IAllowAnonymous>()
                    .Any()
            )
            {
                // An empty security requirement object explicitly permits anonymous access.
                // The OpenAPI writer omits an entirely empty security collection.
                operation.Security = new List<OpenApiSecurityRequirement>
                {
                    new OpenApiSecurityRequirement(),
                };
            }
        }
    }
}
