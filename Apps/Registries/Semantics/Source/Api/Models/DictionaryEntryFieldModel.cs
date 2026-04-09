namespace Adr.Semantics.Models
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using Adr.Semantics.Configuration.Addons.Swagger;

    /// <summary>
    /// Represents a single Dictionary Entry information for a given openapi spec.
    /// </summary>
    public class DictionaryEntryFieldModel : BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the field name.
        /// </summary>
        public required string FieldName { get; set; }

        /// <summary>
        /// Gets or sets the field description.
        /// </summary>
        public required string FieldDescription { get; set; }

        /// <summary>
        /// Gets or sets the schema name and table name.
        /// </summary>
        public required string SchemaNameTableName { get; set; }

        /// <summary>
        /// Gets or sets the data source.
        /// </summary>
        public required string DataSource { get; set; }

        /// <summary>
        /// Gets or sets the data type.
        /// </summary>
        public required string DataType { get; set; }

        /// <summary>
        /// Gets or sets the key relationships.
        /// </summary>
        public required string KeyRelationships { get; set; }

        /// <summary>
        /// Gets or sets the system of record.
        /// </summary>
        public required string SystemOfRecord { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the field is designated as required.
        /// </summary>
        public required string DesignatedAsRequired { get; set; }

        /// <summary>
        /// Gets or sets a hyperlink to the glossary term this field references.
        /// Empty when the field has no semantic reference.
        /// </summary>
        public required string SemanticTermRef { get; set; }
    }
}
