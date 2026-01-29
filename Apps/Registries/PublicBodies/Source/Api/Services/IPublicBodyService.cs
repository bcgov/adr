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
        /// Gets the information for all public body names.
        /// </summary>
        /// <returns>A list public body names.</returns>
        IEnumerable<PublicBodyNameModel> GetAllNames();

        /// <summary>
        /// Gets the information for all public body types.
        /// </summary>
        /// <returns>A list public body types.</returns>
        IEnumerable<PublicBodyTypeModel> GetAllTypes();
    }
}
