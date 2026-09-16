namespace Adr.Semantics.Configuration.Addons.Swagger
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using System.Text.RegularExpressions;
    using Microsoft.OpenApi.Any;
    using Microsoft.OpenApi.Models;
    using Swashbuckle.AspNetCore.SwaggerGen;

    /// <summary>
    /// Adds descriptive metadata and representative examples to the generated contract.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed partial class OpenApiDocumentationFilter
        : IDocumentFilter,
            IOperationFilter,
            ISchemaFilter
    {
        private static readonly Dictionary<string, string> _parameterDescriptions =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["draftId"] = "The unique identifier of the glossary draft.",
                ["glossaryVersion"] = "The semantic version of the glossary release.",
                ["status"] = "Filters drafts by the Draft, Published, or Stale lifecycle status.",
                ["term"] = "The human-readable glossary term slug.",
                ["termVersion"] = "The monotonically increasing version of the term.",
            };

        private static readonly Dictionary<string, string> _responseDescriptions =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["200"] = "The request completed successfully.",
                ["201"] = "The glossary resource was created successfully.",
                ["204"] = "The request completed successfully with no response content.",
                ["400"] = "The request was invalid and could not be processed.",
                ["401"] = "Authentication is required to access this resource.",
                ["403"] = "The authenticated caller is not permitted to access this resource.",
                ["404"] = "The requested glossary resource was not found.",
                ["409"] = "The request conflicts with the current glossary state.",
                ["422"] = "The request contains glossary content that failed validation.",
                ["500"] = "An unexpected server error prevented the request from completing.",
            };

        /// <inheritdoc/>
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            operation.Summary ??= Humanize(context.MethodInfo.Name);
            if (
                string.IsNullOrWhiteSpace(operation.Description)
                || operation.Description.Length <= operation.Summary.Length
                || operation.Description.Length < 40
            )
            {
                operation.Description =
                    $"{operation.Summary.TrimEnd('.')}. The response contains the requested semantic data or explains why the request could not be completed.";
            }

            foreach (var parameter in operation.Parameters ?? [])
            {
                if (
                    string.IsNullOrWhiteSpace(parameter.Description)
                    || parameter.Description.Length < 25
                )
                {
                    parameter.Description = _parameterDescriptions.TryGetValue(
                        parameter.Name,
                        out var description
                    )
                        ? description
                        : $"Identifies the {Humanize(parameter.Name).ToLowerInvariant()} used by this operation.";
                }
            }

            foreach (var response in operation.Responses)
            {
                if (_responseDescriptions.TryGetValue(response.Key, out var description))
                {
                    response.Value.Description = description;
                }
            }
        }

        /// <inheritdoc/>
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            schema.Description ??= $"Represents {Humanize(context.Type.Name)}.";
        }

        /// <inheritdoc/>
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            swaggerDoc.Tags =
            [
                new OpenApiTag
                {
                    Name = "Dictionary",
                    Description = "Operations for semantic dictionaries derived from API descriptions.",
                },
                new OpenApiTag
                {
                    Name = "Glossary",
                    Description = "Operations for published glossary content and releases.",
                },
                new OpenApiTag
                {
                    Name = "GlossaryDraft",
                    Description = "Operations for staging and reviewing glossary changes.",
                },
            ];

            foreach (var path in swaggerDoc.Paths)
            {
                path.Value.Summary = path.Key.EndsWith(
                    "/Glossary/versions",
                    StringComparison.Ordinal
                )
                    ? "Lists glossary versions or publishes a draft as a new version."
                    : path.Value.Operations.Values.FirstOrDefault()?.Summary
                        ?? $"Operations for {path.Key}.";

            }

            foreach (var schema in swaggerDoc.Components.Schemas.Values)
            {
                FlattenInheritedProperties(schema, swaggerDoc.Components.Schemas);
                if (schema.Description?.Length < 30)
                {
                    schema.Description = $"{schema.Description.TrimEnd('.')} used by the Semantics API.";
                }

                EnsurePropertyDescriptions(schema);
                schema.Example = CreateExample(
                    schema,
                    swaggerDoc.Components.Schemas,
                    new HashSet<string>(StringComparer.Ordinal),
                    0
                );
            }

            // Rebuild composite examples after every referenced component has an example.
            foreach (var schema in swaggerDoc.Components.Schemas.Values)
            {
                schema.Example = CreateExample(
                    schema,
                    swaggerDoc.Components.Schemas,
                    new HashSet<string>(StringComparer.Ordinal),
                    0
                );
            }

            foreach (var operation in swaggerDoc.Paths.Values.SelectMany(path =>
                path.Operations.Values
            ))
            {
                foreach (var parameter in operation.Parameters ?? [])
                {
                    AddExample(parameter.Schema, swaggerDoc.Components.Schemas);
                }

                foreach (var content in operation.RequestBody?.Content.Values ?? [])
                {
                    AddExample(content.Schema, swaggerDoc.Components.Schemas);
                }

                foreach (var response in operation.Responses.Values)
                {
                    foreach (var content in response.Content.Values)
                    {
                        AddExample(content.Schema, swaggerDoc.Components.Schemas);
                    }
                }
            }

            swaggerDoc.Components.Schemas.Remove("Adr.Semantics.Models.BaseAuditModel");
        }

        private static void FlattenInheritedProperties(
            OpenApiSchema schema,
            IDictionary<string, OpenApiSchema> schemas
        )
        {
            foreach (var inherited in schema.AllOf.ToList())
            {
                if (
                    inherited.Reference?.Id is not { } referenceId
                    || !schemas.TryGetValue(referenceId, out var inheritedSchema)
                )
                {
                    continue;
                }

                FlattenInheritedProperties(inheritedSchema, schemas);
                foreach (var property in inheritedSchema.Properties)
                {
                    schema.Properties.TryAdd(property.Key, property.Value);
                }

                foreach (var requiredProperty in inheritedSchema.Required)
                {
                    schema.Required.Add(requiredProperty);
                }

                schema.AllOf.Remove(inherited);
            }
        }

        private static void EnsurePropertyDescriptions(OpenApiSchema schema)
        {
            foreach (var property in schema.Properties)
            {
                if (property.Value.Description?.Length < 25)
                {
                    property.Value.Description =
                        $"{property.Value.Description.TrimEnd('.')} used by the glossary service.";
                }

                EnsurePropertyDescriptions(property.Value);
            }

            foreach (var part in schema.AllOf.Concat(schema.OneOf).Concat(schema.AnyOf))
            {
                EnsurePropertyDescriptions(part);
            }

            if (schema.Items is not null)
            {
                EnsurePropertyDescriptions(schema.Items);
            }

            if (schema.AdditionalProperties is not null)
            {
                EnsurePropertyDescriptions(schema.AdditionalProperties);
            }
        }

        private static void AddExample(
            OpenApiSchema? schema,
            IDictionary<string, OpenApiSchema> schemas
        )
        {
            if (schema is not null)
            {
                schema.Example = CreateExample(
                    schema,
                    schemas,
                    new HashSet<string>(StringComparer.Ordinal),
                    0
                );
            }
        }

        private static IOpenApiAny CreateExample(
            OpenApiSchema schema,
            IDictionary<string, OpenApiSchema> schemas,
            ISet<string> visitedReferences,
            int depth
        )
        {
            if (depth >= 5)
            {
                return new OpenApiObject();
            }

            if (depth > 0 && schema.Example is not null)
            {
                return schema.Example;
            }

            if (schema.Reference?.Id is { } referenceId)
            {
                var referencePath = new HashSet<string>(
                    visitedReferences,
                    StringComparer.Ordinal
                );
                if (!referencePath.Add(referenceId))
                {
                    return new OpenApiObject();
                }

                if (schemas.TryGetValue(referenceId, out var referencedSchema))
                {
                    return CreateExample(
                        referencedSchema,
                        schemas,
                        referencePath,
                        depth + 1
                    );
                }
            }

            if (schema.Enum.Count > 0)
            {
                return schema.Enum[0];
            }

            if (schema.AllOf.Count > 0)
            {
                var result = new OpenApiObject();
                foreach (var part in schema.AllOf)
                {
                    if (
                        CreateExample(part, schemas, visitedReferences, depth + 1)
                        is OpenApiObject partObject
                    )
                    {
                        foreach (var property in partObject)
                        {
                            result[property.Key] = property.Value;
                        }
                    }
                }

                AddRequiredProperties(
                    result,
                    schema,
                    schemas,
                    visitedReferences,
                    depth
                );

                return result;
            }

            if (schema.OneOf.FirstOrDefault() is { } oneOf)
            {
                return CreateExample(oneOf, schemas, visitedReferences, depth + 1);
            }

            return schema.Type switch
            {
                "boolean" => new OpenApiBoolean(true),
                "integer" => new OpenApiInteger(1),
                "number" => new OpenApiDouble(1.0),
                "array" => new OpenApiArray(),
                "object" => CreateObjectExample(schema, schemas, visitedReferences, depth),
                null when schema.Properties.Count > 0 =>
                    CreateObjectExample(schema, schemas, visitedReferences, depth),
                _ => CreateStringExample(schema),
            };
        }

        private static OpenApiObject CreateObjectExample(
            OpenApiSchema schema,
            IDictionary<string, OpenApiSchema> schemas,
            ISet<string> visitedReferences,
            int depth
        )
        {
            var result = new OpenApiObject();
            AddRequiredProperties(result, schema, schemas, visitedReferences, depth);
            if (result.Count == 0 && schema.Properties.FirstOrDefault() is var property)
            {
                if (!string.IsNullOrEmpty(property.Key))
                {
                    result[property.Key] = CreateExample(
                        property.Value,
                        schemas,
                        new HashSet<string>(visitedReferences, StringComparer.Ordinal),
                        depth + 1
                    );
                }
            }

            if (result.Count == 0 && schema.AdditionalProperties is not null)
            {
                result["key"] = CreateExample(
                    schema.AdditionalProperties,
                    schemas,
                    new HashSet<string>(visitedReferences, StringComparer.Ordinal),
                    depth + 1
                );
            }

            return result;
        }

        private static void AddRequiredProperties(
            OpenApiObject result,
            OpenApiSchema schema,
            IDictionary<string, OpenApiSchema> schemas,
            ISet<string> visitedReferences,
            int depth
        )
        {
            foreach (var propertyName in schema.Required)
            {
                if (schema.Properties.TryGetValue(propertyName, out var property))
                {
                    result[propertyName] = CreateExample(
                        property,
                        schemas,
                        new HashSet<string>(visitedReferences, StringComparer.Ordinal),
                        depth + 1
                    );
                }
            }
        }

        private static OpenApiString CreateStringExample(OpenApiSchema schema)
        {
            return schema.Format switch
            {
                "date" => new OpenApiString("2026-09-14"),
                "date-time" => new OpenApiString("2026-09-14T12:00:00Z"),
                "email" => new OpenApiString("semantic@example.gov.bc.ca"),
                "uri" => new OpenApiString("https://example.gov.bc.ca/resource"),
                "uuid" => new OpenApiString("123e4567-e89b-12d3-a456-426614174000"),
                _ => new OpenApiString("example"),
            };
        }

        private static string Humanize(string value)
        {
            var withoutGenericArity = value.Split('`')[0];
            return WordBoundaryRegex().Replace(withoutGenericArity, " $1").Trim();
        }

        [GeneratedRegex("([A-Z])")]
        private static partial Regex WordBoundaryRegex();
    }
}
