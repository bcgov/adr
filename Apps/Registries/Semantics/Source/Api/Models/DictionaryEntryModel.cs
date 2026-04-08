namespace Adr.Semantics.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Adr.Semantics.Configuration.Addons.Swagger;

    /// <summary>
    /// Represents Dictionary Entry information for a given openapi spec.
    /// </summary>
    public class DictionaryEntryModel : BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the id of this record
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// Gets or sets the name for the dictionary
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the source for the dictionary
        /// </summary>
        public required string Source { get; set; }

        /// <summary>
        /// Gets or sets the fields for the dictionary entry.
        /// </summary>
        public IList<DictionaryEntryFieldModel> Fields { get; set; } = new List<DictionaryEntryFieldModel>();
    }
}
