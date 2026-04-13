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
        public IEnumerable<PublicBodyModel> GetAll()
        {
            var publicBodies = _publicBodyProvider.GetAllPublicBodies();
            var types = _publicBodyProvider.GetAllTypes().ToList();

            // load the types for each name
            foreach (var publicBody in publicBodies)
            {
                publicBody.PublicBodyType = types.Find(t => t.StaticId == publicBody.TypeId);
            }
            return _publicBodyProvider.GetAllPublicBodies();
        }

        /// <inheritdoc/>
        public PublicBodyModel? GetPublicBody(string id)
        {
            var publicBodies = _publicBodyProvider.GetAllPublicBodies();
            var types = _publicBodyProvider.GetAllTypes().ToList();

            var publicBody = publicBodies.FirstOrDefault(x => x.StaticId == id);
            if (publicBody == null)
            {
                return null;
            }

            publicBody.PublicBodyType = types.Find(t => t.PublicBodyTypeId == publicBody.TypeId);
            return publicBody;
        }

        /// <inheritdoc/>
        IEnumerable<PublicBodyTypeModel> IPublicBodyService.GetAllTypes()
        {
            return _publicBodyProvider.GetAllTypes();
        }
    }
}
