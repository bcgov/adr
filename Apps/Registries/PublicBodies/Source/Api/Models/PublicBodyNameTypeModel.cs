using Adr.PublicBodies.Configuration.Addons.Swagger;

namespace Adr.PublicBodies.Models
{
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
        public required string StaticId { get; set; }

        /// <summary>
        /// Gets or sets the public body type code
        /// </summary>
        [FieldName("Code")]
        [FieldDescription("A code representing the type of public body")]
        [SchemaAndTableName("dbo.PublicBodyType")]
        [DataSource("Database")]
        [DataType("string")]
        [SystemOfRecord("Internal Database")]
        [Required("true")]
        public required string Code { get; set; }

        /// <summary>
        /// Gets or sets the public body type name
        /// </summary>
        [FieldName("Name")]
        [FieldDescription("The human-readable name of the public body type")]
        [SchemaAndTableName("dbo.PublicBodyType")]
        [DataSource("Database")]
        [DataType("string")]
        [SystemOfRecord("Internal Database")]
        [Required("true")]
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the type code description
        /// </summary>
        [FieldName("Description")]
        [FieldDescription("A brief explanation or definition of the public body type")]
        [SchemaAndTableName("dbo.PublicBodyType")]
        [DataSource("Database")]
        [DataType("string")]
        [SystemOfRecord("Internal Database")]
        [Required("false")]
        public string Description { get; set; } = "";
    }
}
