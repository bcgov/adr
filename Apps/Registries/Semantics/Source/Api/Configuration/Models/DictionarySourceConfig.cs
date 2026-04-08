namespace Adr.Semantics.Configuration.Models
{
    /// <summary>
    /// Settings for a single OpenAPI dictionary source under the
    /// <c>DictionarySources</c> configuration section.
    /// </summary>
    public class DictionarySourceConfig
    {
        /// <summary>
        /// Gets or sets the logical name of this source. Populated from the
        /// configuration section key, not bound from the JSON body.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether this source should be fetched.
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Gets or sets the URL of the OpenAPI spec to fetch (e.g. a swagger.json endpoint).
        /// Defaults to empty; entries with no URL are filtered out before use.
        /// </summary>
        public string SourceUrl { get; set; } = string.Empty;
    }
}
