namespace Adr.PublicBodies.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// Represents the lineage history of a public body as a directed acyclic graph.
    /// </summary>
    public class PublicBodyHistoryModel
    {
        /// <summary>
        /// Gets or sets the static ID of the public body that was queried.
        /// </summary>
        public required string PublicBodyId { get; set; }

        /// <summary>
        /// Gets or sets the public bodies in the lineage graph.
        /// </summary>
        public required IEnumerable<PublicBodyModel> PublicBodies { get; set; }

        /// <summary>
        /// Gets or sets the relationships in the lineage graph.
        /// </summary>
        public required IEnumerable<PublicBodyParentChildModel> Relationships { get; set; }
    }
}
