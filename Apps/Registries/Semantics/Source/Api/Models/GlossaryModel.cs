namespace Adr.Semantics.Models
{
    using System.Collections.Generic;
    using System.Text.Json.Serialization;
    using Adr.Semantics.Configuration.Addons.Swagger;

    /// <summary>
    /// Represents Glossary information.
    /// </summary>
    public class GlossaryModel : BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the stable static identifier (GUID) for this term.
        /// Serialized as <c>id</c> so consumers can reference terms by their stable identifier.
        /// </summary>
        [JsonPropertyName("id")]
        public string StaticId { get; set; } = "";

        /// <summary>
        /// Gets or sets the URL-friendly name (slug) for this term.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the term.
        /// </summary>
        public required string Term { get; set; }

        /// <summary>
        /// Gets or sets the published definition (source definition).
        /// </summary>
        public string Definition { get; set; } = "";

        /// <summary>
        /// Gets or sets the keywords for the term.
        /// </summary>
        public IList<string> Keywords { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the scope text for the term.
        /// </summary>
        public string Scope { get; set; } = "";

        /// <summary>
        /// Gets or sets the scope hyperlink URL for the term.
        /// </summary>
        public string ScopeUrl { get; set; } = "";

        /// <summary>
        /// Gets or sets the citation / reference URL for the term.
        /// </summary>
        public string Citations { get; set; } = "";

        /// <summary>
        /// Gets or sets the internal team source (transitional).
        /// </summary>
        public string TeamSource { get; set; } = "";

        /// <summary>
        /// Gets or sets a flag indicating whether the definition has been verified (internal - transitional).
        /// </summary>
        public bool VerifiedDefinitionFlag { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether this term should be published to DevHub (internal - transitional).
        /// </summary>
        public bool PublishToDevHub { get; set; }
    }
}
