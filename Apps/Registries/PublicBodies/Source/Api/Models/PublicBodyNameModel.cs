namespace Adr.PublicBodies.Models
{
    using System.ComponentModel;
    using Adr.PublicBodies.Configuration.Addons.Swagger;

    /// <summary>
    /// Represents ministry information.
    /// </summary>
    public class PublicBodyNameModel : BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the id of this record
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// Gets or sets the id that is DB independent
        /// </summary>
        public required string StaticId { get; set; }

        /// <summary>
        /// Gets or sets the ministry name
        /// </summary>
        [Description("The unique identifier for the product.")]
        [SemanticRefAttribute("http://example.com/ontology/naming")]
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the ministry's acronym
        /// </summary>
        [Description("The unique identifier for the product.")]
        [SemanticRefAttribute("http://example.com/ontology/abreviation")]
        public string Acronym { get; set; } = "";

        /// <summary>
        /// Gets or sets the ministry's acronym
        /// </summary>
        [Description("The unique identifier for the product.")]
        [SemanticRefAttribute("http://example.com/ontology/code-type")]
        public string PublicBodyTypeId { get; set; } = "";

        /// <summary>
        /// Gets or sets the ministry's acronym
        /// </summary>
        [Description("The unique identifier for the product.")]
        public PublicBodyTypeModel? PublicBodyType { get; set; } = null;
    }
}
