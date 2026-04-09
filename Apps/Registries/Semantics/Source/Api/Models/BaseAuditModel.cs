namespace Adr.Semantics.Models
{
    using System;
    using Adr.Semantics.Configuration.Addons.Swagger;

    /// <summary>
    /// Represents a base model with information that can be audited.
    /// </summary>
    public class BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the record created datetime
        /// </summary>
        public DateTime? RecordCreatedDatetime { get; set; }

        /// <summary>
        /// Gets or sets the record ended datetime
        /// </summary>
        public DateTime? RecordEndedDatetime { get; set; }

        /// <summary>
        /// Gets or sets the record created user
        /// </summary>
        public string RecordCreatedUser { get; set; } = "";

        /// <summary>
        /// Gets or sets the record ended user
        /// </summary>
        public string RecordEndedUser { get; set; } = "";
    }
}
