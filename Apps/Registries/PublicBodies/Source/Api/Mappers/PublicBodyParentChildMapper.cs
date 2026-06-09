namespace Adr.PublicBodies.Mappers
{
    using Adr.PublicBodies.Models;
    using CsvHelper.Configuration;

    /// <summary>
    /// Performs a mapping from the read file to the model object.
    /// </summary>
    public sealed class PublicBodyParentChildMapper : ClassMap<PublicBodyParentChildModel>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PublicBodyParentChildMapper"/> class.
        /// </summary>
        public PublicBodyParentChildMapper()
        {
            this.Map(m => m.ParentChildId).Name("PARENT_CHILD_ID");
            this.Map(m => m.TransitionDatetime).Name("TRANSITION_DATETIME");
            this.Map(m => m.ParentUniqueId).Name("PARENT_UNIQUE_ID");
            this.Map(m => m.ChildUniqueId).Name("CHILD_UNIQUE_ID");
            this.Map(m => m.WasRenamed).Ignore();
            this.Map(m => m.WasMerged).Ignore();
            this.Map(m => m.WasSplit).Ignore();
            this.Map(m => m.RecordCreatedDatetime).Name("RECORD_CREATED_DATETIME");
            this.Map(m => m.RecordEndedDatetime).Name("RECORD_ENDED_DATETIME");
            this.Map(m => m.RecordCreatedUser).Name("RECORD_CREATED_USER");
            this.Map(m => m.RecordEndedUser).Name("RECORD_ENDED_USER");
        }
    }
}
