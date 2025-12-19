namespace Adr.PublicBodies.Services
{
    using System.Collections.Generic;
    using Adr.PublicBodies.Models;

    /// <summary>
    /// Interface for a service that manages Ministry information.
    /// </summary>
    public interface IMinistryService
    {
        /// <summary>
        /// Gets the information for all the ministries.
        /// </summary>
        /// <returns>A list of ministries.</returns>
        IEnumerable<MinistryModel> GetAll();
    }
}
