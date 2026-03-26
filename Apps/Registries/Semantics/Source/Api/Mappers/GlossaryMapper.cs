namespace Adr.Semantics.Mappers
{
    using Adr.Semantics.Models;
    using CsvHelper.Configuration;

    /// <summary>
    /// Performs a mapping from the read file to the model object.
    /// </summary>
    public sealed class GlossaryMapper : ClassMap<GlossaryModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GlossaryMapper"/> class.
        /// </summary>
        public GlossaryMapper()
        {
            //  TODO: map the fields
            this.Map(m => m.Id).Name("pb_unique_id");
        }
    }
}
