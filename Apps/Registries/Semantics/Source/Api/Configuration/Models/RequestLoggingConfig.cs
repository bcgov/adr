namespace Adr.Semantics.Configuration.Models
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using OpenTelemetry.Exporter;

    /// <summary>
    /// Settings to control request logging.
    /// </summary>
    public class RequestLoggingConfig
    {
        /// <summary>
        /// Gets or sets a value indicating whether Open Telemetry is enabled.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the optional request paths to exclude, including wildcard prefixes or suffixes.
        /// </summary>
        public IEnumerable<string>? ExcludedPaths { get; set; }
    }
}
