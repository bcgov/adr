namespace Adr.PublicBodies.Models
{
    /// <summary>
    /// Represents public body types.
    /// </summary>
    public class PublicBodyTypeModel : BaseAuditModel
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
        /// Gets or sets the public body type code
        /// </summary>
        public required string Code { get; set; }

        /// <summary>
        /// Gets or sets the public body type code
        /// </summary>
        public required string Name { get; set; }

        ///
        /// <summary>
        /// Gets or sets the type code description
        /// </summary>
        public string Description { get; set; } = "";
    }
}
