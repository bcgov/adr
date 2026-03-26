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
                var memberInfo = context.MemberInfo;

                // FieldNameAttribute
                var fieldAttr = memberInfo.GetCustomAttribute<FieldNameAttribute>();
                if (fieldAttr != null)
                {
                    schema.Extensions.Add("x-bc-field", new OpenApiString(fieldAttr.FieldName));
                }

                // FieldDescriptionAttribute
                var descAttr = memberInfo.GetCustomAttribute<FieldDescriptionAttribute>();
                if (descAttr != null)
                {
                    schema.Extensions.Add(
                        "x-bc-desc",
                        new OpenApiString(descAttr.FieldDescription)
                    );
                }

                // SchemaAndTableNameAttribute
                var schemaTableAttr = memberInfo.GetCustomAttribute<SchemaAndTableNameAttribute>();
                if (schemaTableAttr != null)
                {
                    schema.Extensions.Add(
                        "x-bc-schema-table",
                        new OpenApiString(schemaTableAttr.SchemaAndTableName)
                    );
                }

                // DataSourceAttribute
                var sourceAttr = memberInfo.GetCustomAttribute<DataSourceAttribute>();
                if (sourceAttr != null)
                {
                    schema.Extensions.Add("x-bc-source", new OpenApiString(sourceAttr.DataSource));
                }

                // DataTypeAttribute
                var typeAttr = memberInfo.GetCustomAttribute<DataTypeAttribute>();
                if (typeAttr != null)
                {
                    schema.Extensions.Add("x-bc-type", new OpenApiString(typeAttr.DataType));
                }

                // KeyRelationshipsAttribute
                var keyAttr = memberInfo.GetCustomAttribute<KeyRelationshipsAttribute>();
                if (keyAttr != null)
                {
                    schema.Extensions.Add("x-bc-key", new OpenApiString(keyAttr.KeyRelationships));
                }

                // SystemOfRecordAttribute
                var systemAttr = memberInfo.GetCustomAttribute<SystemOfRecordAttribute>();
                if (systemAttr != null)
                {
                    schema.Extensions.Add("x-bc-sor", new OpenApiString(systemAttr.SystemOfRecord));
                }

                // RequiredAttribute
                var requiredAttr = memberInfo.GetCustomAttribute<RequiredAttribute>();
                if (requiredAttr != null)
                {
                    schema.Extensions.Add("x-bc-req", new OpenApiString(requiredAttr.Required));
                }
            }
        }
    }
}
