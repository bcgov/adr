namespace Adr.PublicBodies.Configuration.Addons.Swagger
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Name of the Technical field.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property)]
    public class FieldNameAttribute : Attribute
    {
        public string FieldName { get; set; }

        public FieldNameAttribute(string field)
        {
            FieldName = field;
        }
    }

    /// <summary>
    /// A short description of the Technical field.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property)]
    public class FieldDescriptionAttribute : Attribute
    {
        public string FieldDescription { get; set; }

        public FieldDescriptionAttribute(string description)
        {
            FieldDescription = description;
        }
    }

    /// <summary>
    /// Structural identifiers that define where the data resides in a database.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property)]
    public class SchemaAndTableNameAttribute : Attribute
    {
        public string SchemaAndTableName { get; set; }

        public SchemaAndTableNameAttribute(string schemaAndTable)
        {
            SchemaAndTableName = schemaAndTable;
        }
    }

    /// <summary>
    /// The origin of the data, such as a database, API, or file system.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property)]
    public class DataSourceAttribute : Attribute
    {
        public string DataSource { get; set; }

        public DataSourceAttribute(string source)
        {
            DataSource = source;
        }
    }

    /// <summary>
    /// The format of the data element (e.g., string, integer, date).
    /// </summary>
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property)]
    public class DataTypeAttribute : Attribute
    {
        public string DataType { get; set; }

        public DataTypeAttribute(string type)
        {
            DataType = type;
        }
    }

    /// <summary>
    /// Logical links between tables that define relational integrity.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property)]
    public class KeyRelationshipsAttribute : Attribute
    {
        public string KeyRelationships { get; set; }

        public KeyRelationshipsAttribute(string relationships)
        {
            KeyRelationships = relationships;
        }
    }

    /// <summary>
    /// The authoritative source system for a given dataset.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property)]
    public class SystemOfRecordAttribute : Attribute
    {
        public string SystemOfRecord { get; set; }

        public SystemOfRecordAttribute(string system)
        {
            SystemOfRecord = system;
        }
    }

    /// <summary>
    /// Indicates whether the attribute is mandatory for compliance or processing.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property)]
    public class RequiredAttribute : Attribute
    {
        public string Required { get; set; }

        public RequiredAttribute(string required)
        {
            Required = required;
        }
    }

    /// <summary>
    /// Associates a property with a glossary term. The value is the term's
    /// <c>StaticId</c> &#8212; the stable GUID identifier from the Semantics glossary
    /// &#8212; which is immune to term/slug renames.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property)]
    public class SemanticAttribute : Attribute
    {
        public string SemanticReference { get; set; }

        public SemanticAttribute(string semanticReference)
        {
            SemanticReference = semanticReference;
        }
    }
}
