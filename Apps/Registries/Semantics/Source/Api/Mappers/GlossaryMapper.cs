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
            this.Map(m => m.StaticId).Name("StaticId");

            this.Map(m => m.Name).Name("Name");

            this.Map(m => m.Term).Name("Term");

            this.Map(m => m.Definition).Name("Published Definition");

            this.Map(m => m.Keywords).Name("Keywords").TypeConverter<ListStringConverter>();

            this.Map(m => m.Context).Name("Context");

            this.Map(m => m.Citations).Name("Citations");

            this.Map(m => m.TeamSource).Name("Team Source - Temp");

            this.Map(m => m.VerifiedDefinitionFlag)
                .Name("Verified Definition")
                .TypeConverter<BooleanFromYesNoConverter>();

            this.Map(m => m.PublishToDevHub)
                .Name("Publish to DevHub")
                .TypeConverter<BooleanFromYesNoConverter>();
        }
    }
}
