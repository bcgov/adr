namespace Adr.Semantics.Providers
{
    using System;
    using System.Collections.Generic;

    internal sealed class GlossaryReleaseCatalog
    {
        public required string Id { get; set; }

        public required string Name { get; set; }

        public required IList<GlossaryImportDefinition> Imports { get; set; }
    }

    internal sealed class GlossaryImportDefinition
    {
        public required DateOnly PublishedAt { get; set; }

        public required string Asset { get; set; }
    }
}
