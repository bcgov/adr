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
            this.Map(m => m.StaticId).Name("PB_UNIQUE_ID");
            this.Map(m => m.PublicBodyId).Name("PUBLIC_BODY_ID");
            this.Map(m => m.BusinessIdSource).Name("BUSINESS_ID_SOURCE");
            this.Map(m => m.BusinessIdValue).Name("BUSINESS_ID_VALUE");
            this.Map(m => m.Name).Name("NAME");
            this.Map(m => m.Acronym).Name("ACRONYM");
            this.Map(m => m.Sector).Name("SECTOR");
            this.Map(m => m.TypeId).Name("TYPE_ID");
            this.Map(m => m.PublicBodyEffectiveDate).Name("PUBLIC_BODY_EFFECTIVE_DATETIME");
            this.Map(m => m.PublicBodyRetiredDate).Name("PUBLIC_BODY_RETIRED_DATETIME");
            this.Map(m => m.RecordCreatedDatetime).Name("RECORD_CREATED_DATETIME");
            this.Map(m => m.RecordEndedDatetime).Name("RECORD_ENDED_DATETIME");
            this.Map(m => m.RecordCreatedUser).Name("RECORD_CREATED_USER");
            this.Map(m => m.RecordEndedUser).Name("RECORD_ENDED_USER");
        }
    }
}
