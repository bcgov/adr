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
        /// Gets the information for all public bodies.
        /// </summary>
        /// <returns>A list public bodies.</returns>
        IEnumerable<PublicBodyModel> GetAll();

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
    }
}
