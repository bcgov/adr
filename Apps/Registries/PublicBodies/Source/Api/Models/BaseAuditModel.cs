namespace Adr.PublicBodies.Models
{
    using Adr.PublicBodies.Configuration.Addons.Swagger;

    /// <summary>
    /// Represents a base model with information that can be audited.
    /// </summary>
    public class BaseAuditModel
    {
        /// <summary>
        /// Gets or sets the effective date. When the data became active
        /// </summary>
        public System.DateOnly? EffectiveDate { get; set; }

        /// <summary>
        /// Gets or sets the retirment date for the information. When is no longer active.
        /// </summary>
        public System.DateOnly? RetirementDate { get; set; }
    }
}
