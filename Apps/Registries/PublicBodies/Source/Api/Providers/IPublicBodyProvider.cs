namespace Adr.PublicBodies.Providers
{
    using System.Collections.Generic;
    using Adr.PublicBodies.Models;

    /// <summary>
    /// Interface for a service that provides public body names.
    /// </summary>
    public interface IPublicBodyProvider
    {
        /// <summary>
        /// Gets all the public body names.
        /// </summary>
        /// <returns>A list of public body names.</returns>
        IEnumerable<PublicBodyNameModel> GetAllNames();

        /// <summary>
        /// Gets all the public body types.
        /// </summary>
        /// <returns>A list public body types.</returns>
        IEnumerable<PublicBodyTypeModel> GetAllTypes();
    }
}
