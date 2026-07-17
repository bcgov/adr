namespace Adr.PublicBodies.Models
{
    using System;
    using Adr.PublicBodies.Configuration.Addons.Swagger;

    /// <summary>
    /// Represents public body types.
    /// </summary>
    public class PublicBodyTypeModel : BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the id of this record
        /// </summary>
        [FieldName("Id")]
        [FieldDescription("Unique identifier for the record")]
        [SchemaAndTableName("dbo.PublicBodyType")]
        [DataSource("Database")]
        [DataType("string")]
        [SystemOfRecord("Internal Database")]
        [Required("true")]
        [Semantic("ea050745-5a9f-4c7b-b4ef-2b750bed999b")] // immutable
        public required string Id { get; set; }

        /// <summary>
        /// Gets or sets the id that is DB independent
        /// </summary>
        [FieldName("StaticId")]
        [FieldDescription("A static, database-independent identifier")]
        [SchemaAndTableName("dbo.PublicBodyType")]
        [DataSource("Database")]
        [DataType("string")]
        [SystemOfRecord("Internal Database")]
        [Required("true")]
        [Semantic("ea050745-5a9f-4c7b-b4ef-2b750bed999b")] // immutable
        public required string StaticId { get; set; }

        /// <summary>
        /// Gets or sets the public body type identifier
        /// </summary>
        [FieldName("PublicBodyTypeId")]
        [FieldDescription("Identifier for the public body type.")]
        [SchemaAndTableName("dbo.PublicBodyType")]
        [DataType("string")]
        [Semantic("ea050745-5a9f-4c7b-b4ef-2b750bed999b")] // immutable
        public string PublicBodyTypeId { get; set; } = "";

        /// <summary>
        /// Gets or sets the public body type name
        /// </summary>
        [FieldName("Name")]
        [FieldDescription("The full name of the public body type")]
        [SchemaAndTableName("dbo.PublicBodyType")]
        [DataSource("Database")]
        [DataType("string")]
        [SystemOfRecord("Internal Database")]
        [Required("true")]
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the public body type short name
        /// </summary>
        [FieldName("ShortName")]
        [FieldDescription("The short name or abbreviation of the public body type")]
        [SchemaAndTableName("dbo.PublicBodyType")]
        [DataSource("Database")]
        [DataType("string")]
        [SystemOfRecord("Internal Database")]
        public string ShortName { get; set; } = "";

        /// <summary>
        /// Gets or sets the type effective datetime
        /// </summary>
        [FieldName("TypeEffectiveDatetime")]
        [FieldDescription("The date and time the public body type became effective.")]
        [SchemaAndTableName("dbo.PublicBodyType")]
        [DataType("string")]
        public DateOnly? TypeEffectiveDatetime { get; set; } = null;

        /// <summary>
        /// Gets or sets the type retired datetime
        /// </summary>
        [FieldName("TypeRetiredDatetime")]
        [FieldDescription("The date and time the public body type was retired.")]
        [SchemaAndTableName("dbo.PublicBodyType")]
        [DataType("string")]
        public DateOnly? TypeRetiredDatetime { get; set; } = null;
    }
}
