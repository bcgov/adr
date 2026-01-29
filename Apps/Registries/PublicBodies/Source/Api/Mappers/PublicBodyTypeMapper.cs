namespace Adr.PublicBodies.Mappers
{
    using Adr.PublicBodies.Models;
    using CsvHelper.Configuration;

    /// <summary>
    /// Performs a mapping from the read file to the model object.
    /// </summary>
    public sealed class PublicBodyTypeMapper : ClassMap<PublicBodyTypeModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PublicBodyTypeMapper"/> class.
        /// </summary>
        public PublicBodyTypeMapper()
        {
            //this.Map(m => m.Id).Default(Guid.NewGuid().ToString());
            this.Map(m => m.StaticId).Name("public_body_type_id");
            this.Map(m => m.Code).Name("public_body_type");
            this.Map(m => m.Name).Name("public_body_type_short_name");
            this.Map(m => m.Description).Name("public_body_type_description");
            this.Map(m => m.EffectiveDate).Name("effective_date");
            this.Map(m => m.RetirementDate).Name("last_effective_date");
            //this.Map(m => m.LastUpdateDateTime).Name("last_update_datetime");
        }
    }
}
