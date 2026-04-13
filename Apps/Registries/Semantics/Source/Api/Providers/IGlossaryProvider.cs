namespace Adr.Semantics.Providers
{
    using System.Collections.Generic;
    using Adr.Semantics.Models;

    /// <summary>
    /// Interface for a provider that provides glossary information.
    /// </summary>
    public interface IGlossaryProvider
    {
        /// <summary>
        /// Gets all the glossaries.
        /// </summary>
        /// <returns>A list of glossaries.</returns>
        IEnumerable<GlossaryModel> GetAllGlossaries();
    }
}
