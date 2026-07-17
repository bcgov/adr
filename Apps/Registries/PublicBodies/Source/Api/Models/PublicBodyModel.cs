namespace Adr.PublicBodies.Models
{
    using System;
    using System.ComponentModel;
    using Adr.PublicBodies.Configuration.Addons.Swagger;

    /// <summary>
    /// Represents ministry information.
    /// </summary>
    public class PublicBodyModel : BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the id of this record
        /// </summary>
        [FieldName("Id")]
        [FieldDescription("Unique identifier for the record in the database.")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        [Required("true")]
        [Semantic("ea050745-5a9f-4c7b-b4ef-2b750bed999b")] // immutable
        public required string Id { get; set; }

        /// <summary>
        /// Gets or sets the id that is DB independent
        /// </summary>
        [FieldName("StaticId")]
        [FieldDescription("A database-independent identifier for the public body.")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        [Required("true")]
        [Semantic("ea050745-5a9f-4c7b-b4ef-2b750bed999b")] // immutable
        public required string StaticId { get; set; }

        /// <summary>
        /// Gets or sets the public body identifier
        /// </summary>
        [FieldName("PublicBodyId")]
        [FieldDescription("Identifier for the public body.")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        [Semantic("ea050745-5a9f-4c7b-b4ef-2b750bed999b")] // immutable
        public string PublicBodyId { get; set; } = "";

        /// <summary>
        /// Gets or sets the business id source
        /// </summary>
        [FieldName("BusinessIdSource")]
        [FieldDescription("The source system for the business identifier.")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        [Semantic("b2c5967a-33ea-47a5-88e1-7d20ec7f0994")] // system-of-record
        public string BusinessIdSource { get; set; } = "";

        /// <summary>
        /// Gets or sets the business id value
        /// </summary>
        [FieldName("BusinessIdValue")]
        [FieldDescription("The business identifier value from the source system.")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        public string BusinessIdValue { get; set; } = "";

        /// <summary>
        /// Gets or sets the ministry name
        /// </summary>
        [FieldName("Name")]
        [FieldDescription("The full name of the public body or ministry.")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        [Required("true")]
        [SystemOfRecord("Ministry Database")]
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the acronym
        /// </summary>
        [FieldName("Acronym")]
        [FieldDescription("The short name or abbreviation of the public body.")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        public string Acronym { get; set; } = "";

        /// <summary>
        /// Gets or sets the sector
        /// </summary>
        [FieldName("Sector")]
        [FieldDescription("The sector the public body belongs to.")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        public string Sector { get; set; } = "";

        /// <summary>
        /// Gets or sets the public body type identifier
        /// </summary>
        [FieldName("TypeId")]
        [FieldDescription("Identifier for the type of public body (e.g., Ministry, Agency).")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        [DataSource("PublicBodyType table")]
        [KeyRelationships("Foreign key to PublicBodyType")]
        [Semantic("ea050745-5a9f-4c7b-b4ef-2b750bed999b")] // immutable
        public string TypeId { get; set; } = "";

        /// <summary>
        /// Gets or sets the public body effective datetime
        /// </summary>
        [FieldName("PublicBodyEffectiveDatetime")]
        [FieldDescription("The date and time the public body became effective.")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        public DateOnly? PublicBodyEffectiveDate { get; set; } = null;

        /// <summary>
        /// Gets or sets the public body retired datetime
        /// </summary>
        [FieldName("PublicBodyRetiredDatetime")]
        [FieldDescription("The date and time the public body was retired.")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        public DateOnly? PublicBodyRetiredDate { get; set; } = null;
    }
}
