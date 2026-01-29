namespace Adr.PublicBodies.Controllers
{
    using System;
    using System.Collections.Generic;
    using Adr.PublicBodies.Models;
    using Adr.PublicBodies.Services;
    using Asp.Versioning;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The PublicBodies controller.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("[controller]")]
    [ApiController]
    public class PublicBodiesController : Controller
    {
        private readonly ILogger<PublicBodiesController> _logger;

        private readonly IPublicBodyService _publicBodyService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicBodiesController"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        /// <param name="ministryService">Injected Ministry Service.</param>
        public PublicBodiesController(
            ILogger<PublicBodiesController> logger,
            IPublicBodyService publicBodyService
        )
        {
            _logger = logger;
            _publicBodyService = publicBodyService;
        }

        [HttpGet]
        [Produces("application/json")]
        public BaseResponseModel<String> Index()
        {
            return new BaseResponseModel<String>()
            {
                Payload = "Hello Adr World",
                DatetimeRequested = DateTime.Now,
            };
        }

        [HttpGet("names")]
        [Produces("application/json")]
        public BaseResponseModel<IEnumerable<PublicBodyNameModel>> GetNames()
        {
            var names = _publicBodyService.GetAllNames();
            var requestResponse = new BaseResponseModel<IEnumerable<PublicBodyNameModel>>()
            {
                Payload = names,
                DatetimeRequested = DateTime.Now,
            };

            return requestResponse;
        }

        [HttpGet("types")]
        [Produces("application/json")]
        public BaseResponseModel<IEnumerable<PublicBodyTypeModel>> GetTypes()
        {
            var types = _publicBodyService.GetAllTypes();
            var requestResponse = new BaseResponseModel<IEnumerable<PublicBodyTypeModel>>()
            {
                Payload = types,
                DatetimeRequested = DateTime.Now,
            };

            return requestResponse;
        }
    }
}
