namespace Adr.Semantics.Models
{
    /// <summary>
    /// Represents a glossary response and identifies the vocabulary release used for its payload.
    /// </summary>
    /// <typeparam name="T">The response payload type.</typeparam>
    public class GlossaryResponseModel<T> : BaseResponseModel<T>
    {
        /// <summary>
        /// Gets or sets the glossary release that supplied the payload.
        /// </summary>
        public required GlossaryVersionModel Glossary { get; set; }
    }
}
