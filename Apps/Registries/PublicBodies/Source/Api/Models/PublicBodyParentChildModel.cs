namespace Adr.PublicBodies.Models
{
    using System;
    using Adr.PublicBodies.Configuration.Addons.Swagger;

    /// <summary>
    /// Represents a parent-child relationship between public bodies.
    /// </summary>
    public class PublicBodyParentChildModel : BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the parent-child relationship identifier
        /// </summary>
        [FieldName("ParentChildId")]
        [FieldDescription("Unique identifier for the parent-child relationship record.")]
        [SchemaAndTableName("dbo.PublicBodyParentChild")]
        [DataType("string")]
        [Required("true")]
        public required string ParentChildId { get; set; }

        /// <summary>
        /// Gets or sets the action datetime
        /// </summary>
        [FieldName("ActionDatetime")]
        [FieldDescription("The date and time the action (rename, merge, or split) occurred.")]
        [SchemaAndTableName("dbo.PublicBodyParentChild")]
        [DataType("string")]
        public DateOnly? ActionDatetime { get; set; } = null;

        /// <summary>
        /// Gets or sets the parent unique identifier
        /// </summary>
        [FieldName("ParentUniqueId")]
        [FieldDescription("The unique identifier of the parent public body.")]
        [SchemaAndTableName("dbo.PublicBodyParentChild")]
        [DataType("string")]
        [Required("true")]
        [KeyRelationships("Foreign key to PublicBody")]
        public required string ParentUniqueId { get; set; }

        /// <summary>
        /// Gets or sets the child unique identifier
        /// </summary>
        [FieldName("ChildUniqueId")]
        [FieldDescription("The unique identifier of the child public body.")]
        [SchemaAndTableName("dbo.PublicBodyParentChild")]
        [DataType("string")]
        [Required("true")]
        [KeyRelationships("Foreign key to PublicBody")]
        public required string ChildUniqueId { get; set; }

        /// <summary>
        /// Gets or sets whether the public body was renamed
        /// </summary>
        [FieldName("WasRenamed")]
        [FieldDescription(
            "Indicates whether the relationship involved a rename of the public body."
        )]
        [SchemaAndTableName("dbo.PublicBodyParentChild")]
        [DataType("bool")]
        public bool WasRenamed { get; set; } = false;

        /// <summary>
        /// Gets or sets whether the public body was merged
        /// </summary>
        [FieldName("WasMerged")]
        [FieldDescription("Indicates whether the relationship involved a merge of public bodies.")]
        [SchemaAndTableName("dbo.PublicBodyParentChild")]
        [DataType("bool")]
        public bool WasMerged { get; set; } = false;

        /// <summary>
        /// Gets or sets whether the public body was split
        /// </summary>
        [FieldName("WasSplit")]
        [FieldDescription(
            "Indicates whether the relationship involved a split of the public body."
        )]
        [SchemaAndTableName("dbo.PublicBodyParentChild")]
        [DataType("bool")]
        public bool WasSplit { get; set; } = false;

        /// <summary>
        /// Gets or sets the public body effective datetime
        /// </summary>
        [FieldName("PublicBodyEffectiveDatetime")]
        [FieldDescription("The date and time the public body became effective.")]
        [SchemaAndTableName("dbo.PublicBodyParentChild")]
        [DataType("string")]
        public DateOnly? PublicBodyEffectiveDatetime { get; set; } = null;
    }
}
