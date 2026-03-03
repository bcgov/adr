namespace Adr.PublicBodies.Models
{
    using System.ComponentModel;
    using Adr.PublicBodies.Configuration.Addons.Swagger;

    /// <summary>
    /// Represents ministry information.
    /// </summary>
    public class PublicBodyNameModel : BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the id of this record
        /// </summary>
        [FieldName("Id")]
        [FieldDescription("Unique identifier for the record in the database.")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        [Required("true")]
        public required string Id { get; set; }

        /// <summary>
        /// Gets or sets the id that is DB independent
        /// </summary>
        [FieldName("StaticId")]
        [FieldDescription("A database-independent identifier for the public body.")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        [Required("true")]
        public required string StaticId { get; set; }

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
        /// Gets or sets the ministry's acronym
        /// </summary>
        [FieldName("Acronym")]
        [FieldDescription("The short name or abbreviation of the public body.")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        public string Acronym { get; set; } = "";

        /// <summary>
        /// Gets or sets the ministry's acronym
        /// </summary>
        [FieldName("PublicBodyTypeId")]
        [FieldDescription("Identifier for the type of public body (e.g., Ministry, Agency).")]
        [SchemaAndTableName("dbo.PublicBodies")]
        [DataType("string")]
        [DataSource("PublicBodyType table")]
        [KeyRelationships("Foreign key to PublicBodyType")]
        public string PublicBodyTypeId { get; set; } = "";

        /// <summary>
        /// Gets or sets the ministry's acronym
        /// </summary>
        [FieldDescription("The type of public body (e.g., Ministry, Agency).")]
        [DataSource("PublicBodyType table")]
        public PublicBodyTypeModel? PublicBodyType { get; set; } = null;
    }
}
