namespace Adr.PublicBodies.Models
{
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
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the ministry's acronym
        /// </summary>
        public string Acronym { get; set; } = "";

        /// <summary>
        /// Gets or sets the ministry's acronym
        /// </summary>
        public string PublicBodyTypeId { get; set; } = "";

        /// <summary>
        /// Gets or sets the ministry's acronym
        /// </summary>
        public PublicBodyTypeModel? PublicBodyType { get; set; } = null;
    }
}
