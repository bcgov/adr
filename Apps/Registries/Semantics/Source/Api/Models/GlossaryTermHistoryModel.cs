namespace Adr.Semantics.Models
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json.Serialization;

    /// <summary>
    /// Describes a published glossary term revision or an invalid submission in its audit history.
    /// </summary>
    public sealed class GlossaryTermHistoryModel
    {
        /// <summary>Gets or sets Published or Invalid.</summary>
        public required string Status { get; set; }

        /// <summary>Gets or sets the published term version, when this entry is a revision.</summary>
        public int? Version { get; set; }

        /// <summary>Gets or sets the published term revision.</summary>
        public GlossaryModel? Term { get; set; }

        /// <summary>Gets or sets the submitted term slug.</summary>
        public required string Name { get; set; }

        /// <summary>Gets or sets the submitted UUID.</summary>
        [JsonPropertyName("submittedId")]
        public string SubmittedStaticId { get; set; } = "";

        /// <summary>Gets or sets the source type, such as CSV or API.</summary>
        public required string SourceType { get; set; }

        /// <summary>Gets or sets the source asset or draft reference.</summary>
        public required string SourceReference { get; set; }

        /// <summary>Gets or sets the submitted operation.</summary>
        public required string Operation { get; set; }

        /// <summary>Gets or sets whether the submission declared a breaking change.</summary>
        public bool BreakingChange { get; set; }

        /// <summary>Gets or sets the reasons an invalid submission was rejected.</summary>
        public IReadOnlyList<string> InvalidReasons { get; set; } = [];

        /// <summary>Gets or sets the original submitted data for an invalid entry.</summary>
        public string? SourcePayload { get; set; }

        /// <summary>Gets or sets the glossary releases that contain this term revision.</summary>
        public IReadOnlyList<string> GlossaryVersions { get; set; } = [];

        /// <summary>Gets or sets when this revision or invalid submission was recorded.</summary>
        public DateTime RecordedUtc { get; set; }
    }
}
