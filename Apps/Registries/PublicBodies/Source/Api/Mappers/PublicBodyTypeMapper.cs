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
            this.Map(m => m.StaticId).Name("pbt_unique_id");
            this.Map(m => m.PublicBodyTypeId).Name("public_body_type_id");
            this.Map(m => m.Name).Name("public_body_type");
            this.Map(m => m.ShortName).Name("public_body_type_short_name");
            this.Map(m => m.TypeEffectiveDatetime).Name("type_effective_datetime");
            this.Map(m => m.TypeRetiredDatetime).Name("type_retired_datetime");
            this.Map(m => m.RecordCreatedDatetime).Name("record_created_datetime");
            this.Map(m => m.RecordEndedDatetime).Name("record_ended_datetime");
            this.Map(m => m.RecordCreatedUser).Name("record_created_user");
            this.Map(m => m.RecordEndedUser).Name("record_ended_user");
        }
    }
}
