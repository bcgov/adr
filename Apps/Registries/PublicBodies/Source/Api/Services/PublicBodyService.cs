namespace Adr.PublicBodies.Services
{
    using System.Collections.Generic;
    using System.Linq;
    using Adr.PublicBodies.Models;
    using Adr.PublicBodies.Providers;
    using Microsoft.Extensions.Logging;

    public class PublicBodyService : IPublicBodyService
    {
        private readonly ILogger<PublicBodyService> _logger;
        private readonly IPublicBodyProvider _publicBodyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicBodyService"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        public PublicBodyService(
            ILogger<PublicBodyService> logger,
            IPublicBodyProvider publicBodyProvider
        )
        {
            _logger = logger;
            _publicBodyProvider = publicBodyProvider;
        }

        /// <inheritdoc/>
        public IEnumerable<PublicBodyNameModel> GetAllNames()
        {
            var names = _publicBodyProvider.GetAllNames();
            var types = _publicBodyProvider.GetAllTypes().ToList();

            // load the types for each name
            foreach (var nameRecord in names)
            {
                nameRecord.PublicBodyType = types.Find(t =>
                    t.StaticId == nameRecord.PublicBodyTypeId
                );
            }
            return _publicBodyProvider.GetAllNames();
        }

        /// <inheritdoc/>
        IEnumerable<PublicBodyTypeModel> IPublicBodyService.GetAllTypes()
        {
            return _publicBodyProvider.GetAllTypes();
        }
    }
}
