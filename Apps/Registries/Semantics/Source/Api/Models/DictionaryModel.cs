namespace Adr.Semantics.Models
{
    using System;
    using System.ComponentModel;
    using Adr.Semantics.Configuration.Addons.Swagger;

    /// <summary>
    /// Represents Dictionary information.
    /// </summary>
    public class DictionaryModel : BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the id of this record
        /// </summary>
        public required string Id { get; set; }
    }
}
