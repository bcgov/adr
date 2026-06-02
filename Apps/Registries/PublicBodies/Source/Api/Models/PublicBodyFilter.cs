namespace Adr.PublicBodies.Models
{
    using System;

    /// <summary>
    /// Optional, AND-combined filters for the public bodies list endpoint.
    /// All members are optional; an empty filter returns every public body.
    /// </summary>
    public class PublicBodyFilter
    {
        /// <summary>
        /// Gets or sets a free-text term matched (case-insensitive, contains) against
        /// both <see cref="PublicBodyModel.Name"/> and <see cref="PublicBodyModel.Acronym"/>.
        /// </summary>
        public string? Search { get; set; }

        /// <summary>
        /// Gets or sets an exact, case-insensitive match against <see cref="PublicBodyModel.Sector"/>.
        /// </summary>
        public string? Sector { get; set; }

        /// <summary>
        /// Gets or sets an exact, case-insensitive match against <see cref="PublicBodyModel.TypeId"/>.
        /// </summary>
        public string? TypeId { get; set; }

        /// <summary>
        /// Gets or sets a filter on the derived active status as of today. When true, only bodies
        /// currently effective and not yet retired are returned; when false, only public
        /// bodies not currently active are returned. Ignored when <see cref="ActiveOn"/> is set.
        /// </summary>
        public bool? Active { get; set; }

        /// <summary>
        /// Gets or sets a date on which to evaluate the derived active status. When set, only bodies
        /// active on that date (effective on or before, and not retired by then) are returned.
        /// Takes precedence over <see cref="Active"/>.
        /// </summary>
        public DateOnly? ActiveOn { get; set; }
    }
}
