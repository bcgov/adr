namespace Adr.Semantics.Models
{
    using System.Collections.Generic;
    using System.ComponentModel;
    using Adr.Semantics.Configuration.Addons.Swagger;

    /// <summary>
    /// Represents Dictionary information.
    /// </summary>
    public class DictionaryModel : BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the id of this record.
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// Gets or sets the parsed openapi spec entries that make up this dictionary.
        /// </summary>
        public IList<DictionaryEntryModel> Entries { get; set; } = new List<DictionaryEntryModel>();
    }
}
