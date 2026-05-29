namespace Adr.PublicBodies.Services
{
    using System.Collections.Generic;
    using Adr.PublicBodies.Models;

    /// <summary>
    /// Interface for a service that manages Ministry information.
    /// </summary>
    public interface IPublicBodyService
    {
        /// <summary>
        /// Gets the information for all public bodies, optionally filtered.
        /// </summary>
        /// <param name="filter">Optional, AND-combined filters. When null, all public bodies are returned.</param>
        /// <returns>A list public bodies.</returns>
        IEnumerable<PublicBodyModel> GetAll(PublicBodyFilter? filter = null);

        /// <summary>
        /// Gets the information for public body with a given id.
        /// </summary>
        /// <returns>The matching public body.</returns>
        PublicBodyModel? GetPublicBody(string id);

        /// <summary>
        /// Gets the information for all public body types.
        /// </summary>
        /// <returns>A list public body types.</returns>
        IEnumerable<PublicBodyTypeModel> GetAllTypes();

        /// <summary>
        /// Gets all parent-child relationships between public bodies.
        /// </summary>
        /// <returns>A list of parent-child relationships.</returns>
        IEnumerable<PublicBodyParentChildModel> GetAllParentChildRelationships();

        /// <summary>
        /// Gets the full lineage history for a public body as a DAG.
        /// </summary>
        /// <param name="id">The static ID of the public body.</param>
        /// <returns>The lineage graph, or null if the public body is not found.</returns>
        PublicBodyHistoryModel? GetHistory(string id);
    }
}
