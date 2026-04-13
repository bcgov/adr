namespace Adr.Semantics.Providers
{
    using System.Collections.Generic;
    using Adr.Semantics.Models;

    /// <summary>
    /// Interface for a provider that provides dictionary information.
    /// </summary>
    public interface IDictionaryProvider
    {
        /// <summary>
        /// Gets all the dictionary info.
        /// </summary>
        /// <returns>A list of dictionaries.</returns>
        IEnumerable<DictionaryModel> GetAllDictionaries();
    }
}
