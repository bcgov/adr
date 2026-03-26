namespace Adr.PublicBodies.Mappers
{
    using Adr.PublicBodies.Models;
    using CsvHelper.Configuration;

    /// <summary>
    /// Performs a mapping from the read file to the model object.
    /// </summary>
    public sealed class PublicBodyMapper : ClassMap<PublicBodyModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PublicBodyMapper"/> class.
        /// </summary>
        public PublicBodyMapper()
        {
            this.Map(m => m.StaticId).Name("pb_unique_id");
            this.Map(m => m.PublicBodyId).Name("public_body_id");
            this.Map(m => m.BusinessIdSource).Name("business_id_source");
            this.Map(m => m.BusinessIdValue).Name("business_id_value");
            this.Map(m => m.Name).Name("name");
            this.Map(m => m.Acronym).Name("acronym");
            this.Map(m => m.Sector).Name("sector");
            this.Map(m => m.TypeId).Name("type_id");
            this.Map(m => m.PublicBodyEffectiveDate).Name("public_body_effective_datetime");
            this.Map(m => m.PublicBodyRetiredDate).Name("public_body_retired_datetime");
            this.Map(m => m.RecordCreatedDatetime).Name("record_created_datetime");
            this.Map(m => m.RecordEndedDatetime).Name("record_ended_datetime");
            this.Map(m => m.RecordCreatedUser).Name("record_created_user");
            this.Map(m => m.RecordEndedUser).Name("record_ended_user");
        }
    }
}
