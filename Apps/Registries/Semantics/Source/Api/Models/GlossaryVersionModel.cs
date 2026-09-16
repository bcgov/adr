namespace Adr.Semantics.Models
{
    using System;

    /// <summary>
    /// Identifies an immutable release of the glossary vocabulary.
    /// </summary>
    public class GlossaryVersionModel
    {
        /// <summary>
        /// Gets or sets the stable, human-readable glossary identifier.
        /// </summary>
        public required string Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of the glossary.
        /// </summary>
        public required string Name { get; set; }

        /// <summary>
        /// Gets or sets the semantic version assigned to this glossary release.
        /// </summary>
        public required string Version { get; set; }

        /// <summary>
        /// Gets or sets the date on which this glossary release was published.
        /// </summary>
        public required DateOnly PublishedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this is the current glossary release.
        /// </summary>
        public required bool IsCurrent { get; set; }
    }
}
