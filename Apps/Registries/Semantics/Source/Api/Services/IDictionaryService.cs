namespace Adr.Semantics.Services
{
    using System.Collections.Generic;
    using Adr.Semantics.Models;

    /// <summary>
    /// Interface for a service that manages Dictionary information.
    /// </summary>
    public interface IDictionaryService
    {
        /// <summary>
        /// Gets all the dictionaries.
        /// </summary>
        /// <returns>A list glossaries.</returns>
        IEnumerable<DictionaryModel> GetAll();
    }
}
