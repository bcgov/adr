namespace Adr.PublicBodies.Mappers
{
    using Adr.PublicBodies.Models;
    using CsvHelper.Configuration;

    /// <summary>
    /// Performs a mapping from the read file to the model object.
    /// </summary>
    public sealed class PublicBodyNameMapper : ClassMap<PublicBodyNameModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PublicBodyNameMapper"/> class.
        /// </summary>
        /// <param name="filedownload">The filedownload to map.</param>
        public PublicBodyNameMapper()
        {
            //this.Map(m => m.Id).Default(Guid.NewGuid().ToString());
            this.Map(m => m.StaticId).Name("public_body_id");
            this.Map(m => m.Name).Name("name");
            this.Map(m => m.PublicBodyTypeId).Name("type_id");
            //this.Map(m => m.Acronym).Name("acronym");
            this.Map(m => m.EffectiveDate).Name("effective_date");
            this.Map(m => m.RetirementDate).Name("last_effective_date");
            //this.Map(m => m.LastUpdateDateTime).Name("last_update_datetime");
        }
    }
}
