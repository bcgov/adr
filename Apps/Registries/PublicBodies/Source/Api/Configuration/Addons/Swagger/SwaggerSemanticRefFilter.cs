namespace Adr.PublicBodies.Configuration.Addons.Swagger
{
    using System.Reflection;
    using Microsoft.OpenApi.Any;
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;

    public class SwaggerSemanticRefFilter : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.MemberInfo != null)
            {
                var attr = context.MemberInfo.GetCustomAttribute<SemanticRefAttribute>();
                if (attr != null)
                {
                    schema.Extensions.Add("x-bc-semantic-ref", new OpenApiString(attr.Reference));
                }
            }
        }
    }
}
