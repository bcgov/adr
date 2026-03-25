using Adr.Semantics.Configuration.Addons.Swagger;

namespace Adr.Semantics.Models
{
    /// <summary>
    /// Represents Glossary information.
    /// </summary>
    public class GlossaryModel : BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the id of this record
        /// </summary>
        public required string Id { get; set; }
    }
}
