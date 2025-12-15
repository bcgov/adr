namespace Adr.PublicBodies.Configuration.Models
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
        /// Gets or sets the names of activity sources to monitor.
        /// Gets or sets the optional request paths to exclude, can handle * wildcard in prefix or postfix.</param>
        /// </summary>
        public IEnumerable<string>? ExcludedPaths { get; set; } = null;
    }
}
