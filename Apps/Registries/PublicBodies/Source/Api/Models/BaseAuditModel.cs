namespace Adr.PublicBodies.Models
{
    using System;
    using Adr.PublicBodies.Configuration.Addons.Swagger;

    /// <summary>
    /// Represents a base model with information that can be audited.
    /// </summary>
    public class BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the record created datetime
        /// </summary>
        [FieldName("RecordCreatedDatetime")]
        [FieldDescription("The date and time the record was created.")]
        [DataType("string")]
        public DateOnly? RecordCreatedDatetime { get; set; } = null;

        /// <summary>
        /// Gets or sets the record ended datetime
        /// </summary>
        [FieldName("RecordEndedDatetime")]
        [FieldDescription("The date and time the record was ended.")]
        [DataType("string")]
        public DateOnly? RecordEndedDatetime { get; set; } = null;

        /// <summary>
        /// Gets or sets the record created user
        /// </summary>
        [FieldName("RecordCreatedUser")]
        [FieldDescription("The user who created the record.")]
        [DataType("string")]
        public string RecordCreatedUser { get; set; } = "";

        /// <summary>
        /// Gets or sets the record ended user
        /// </summary>
        [FieldName("RecordEndedUser")]
        [FieldDescription("The user who ended the record.")]
        [DataType("string")]
        public string RecordEndedUser { get; set; } = "";
    }
}
