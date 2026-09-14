namespace Adr.Semantics.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.Json;
    using Adr.Semantics.Data;
    using Adr.Semantics.Models;

    internal static class GlossaryTermPersistence
    {
        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        public static GlossaryTermRevisionEntity ToEntity(GlossaryModel model, int revision)
        {
            return new GlossaryTermRevisionEntity
            {
                StaticId = Guid.Parse(model.StaticId),
                Revision = revision,
                Name = model.Name,
                Term = model.Term,
                Definition = model.Definition,
                Example = model.Example,
                SchemaType = model.SchemaType,
                SchemaConstraintsJson = SerializeConstraints(model.SchemaConstraints),
                KeywordsJson = JsonSerializer.Serialize(model.Keywords, SerializerOptions),
                Scope = model.Scope,
                ScopeUrl = model.ScopeUrl,
                Citations = model.Citations,
                TeamSource = model.TeamSource,
                VerifiedDefinitionFlag = model.VerifiedDefinitionFlag,
                PublishToDevHub = model.PublishToDevHub,
                ContentHash = GetContentHash(model),
            };
        }

        public static GlossaryModel ToModel(GlossaryTermRevisionEntity entity)
        {
            var constraints = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(
                    entity.SchemaConstraintsJson,
                    SerializerOptions
                )
                ?? [];

            return new GlossaryModel
            {
                Version = entity.Revision,
                StaticId = entity.StaticId.ToString(),
                Name = entity.Name,
                Term = entity.Term,
                Definition = entity.Definition,
                Example = entity.Example,
                SchemaType = entity.SchemaType,
                SchemaConstraints = constraints.ToDictionary(
                    item => item.Key,
                    item => (object)item.Value.Clone(),
                    StringComparer.Ordinal
                ),
                Keywords = JsonSerializer.Deserialize<List<string>>(
                        entity.KeywordsJson,
                        SerializerOptions
                    )
                    ?? [],
                Scope = entity.Scope,
                ScopeUrl = entity.ScopeUrl,
                Citations = entity.Citations,
                TeamSource = entity.TeamSource,
                VerifiedDefinitionFlag = entity.VerifiedDefinitionFlag,
                PublishToDevHub = entity.PublishToDevHub,
            };
        }

        public static string GetContentHash(GlossaryModel model)
        {
            var content = new GlossaryTermContent(
                model.Name,
                model.Term,
                model.Definition,
                model.Example,
                model.SchemaType,
                SerializeConstraints(model.SchemaConstraints),
                model.Keywords,
                model.Scope,
                model.ScopeUrl,
                model.Citations,
                model.TeamSource,
                model.VerifiedDefinitionFlag,
                model.PublishToDevHub
            );
            var bytes = SHA256.HashData(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(content, SerializerOptions))
            );
            return Convert.ToHexString(bytes);
        }

        private static string SerializeConstraints(IDictionary<string, object> constraints)
        {
            var ordered = constraints
                .OrderBy(item => item.Key, StringComparer.Ordinal)
                .ToDictionary(
                    item => item.Key,
                    item => JsonSerializer.SerializeToElement(item.Value),
                    StringComparer.Ordinal
                );
            return JsonSerializer.Serialize(ordered, SerializerOptions);
        }

        private sealed record GlossaryTermContent(
            string Name,
            string Term,
            string Definition,
            string Example,
            string SchemaType,
            string SchemaConstraintsJson,
            IList<string> Keywords,
            string Scope,
            string ScopeUrl,
            string Citations,
            string TeamSource,
            bool VerifiedDefinitionFlag,
            bool PublishToDevHub
        );
    }
}
