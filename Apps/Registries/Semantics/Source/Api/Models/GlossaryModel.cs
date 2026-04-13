namespace Adr.Semantics.Models
{
    using System.Collections.Generic;
    using Adr.Semantics.Configuration.Addons.Swagger;

    /// <summary>
    /// Represents Glossary information.
    /// </summary>
    public class GlossaryModel : BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the id of this record
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// Gets or sets the term.
        /// </summary>
        public required string Term { get; set; }

        /// <summary>
        /// Gets or sets the published definition (source definition).
        /// </summary>
        public string Definition { get; set; } = "";

        /// <summary>
        /// Gets or sets the categories of the term.
        /// </summary>
        public IList<string> Categories { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets the source of the term.
        /// </summary>
        public string Source { get; set; } = "";

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
