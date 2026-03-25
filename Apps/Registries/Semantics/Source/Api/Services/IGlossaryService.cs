namespace Adr.Semantics.Services
{
    using System.Collections.Generic;
    using Adr.Semantics.Models;

    /// <summary>
    /// Interface for a service that manages Glossary information.
    /// </summary>
    public interface IGlossaryService
    {
        /// <summary>
        /// Gets all the glossaries.
        /// </summary>
        /// <returns>A list glossaries.</returns>
        IEnumerable<GlossaryModel> GetAll();
    }
}
