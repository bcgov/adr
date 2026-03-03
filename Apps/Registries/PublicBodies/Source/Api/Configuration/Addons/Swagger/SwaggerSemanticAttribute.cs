namespace Adr.PublicBodies.Configuration.Addons.Swagger
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    /// <summary>
    /// Excludes the specified controller.
    /// </summary>
    [ExcludeFromCodeCoverage]
    [AttributeUsage(AttributeTargets.Property)]
    public class SemanticRefAttribute : Attribute
    {
        public string Reference { get; set; }

        public SemanticRefAttribute(string reference)
        {
            Reference = reference;
        }
    }
}
