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

        private readonly IMinistryService _ministryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicBodiesController"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        /// <param name="ministryService">Injected Ministry Service.</param>
        public PublicBodiesController(
            ILogger<PublicBodiesController> logger,
            IMinistryService ministryService
        )
        {
            _logger = logger;
            _ministryService = ministryService;
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

        [HttpGet("ministries")]
        [Produces("application/json")]
        public BaseResponseModel<IEnumerable<MinistryModel>> GetMinistries()
        {
            var ministries = _ministryService.GetAll();
            var requestResponse = new BaseResponseModel<IEnumerable<MinistryModel>>()
            {
                Payload = ministries,
                DatetimeRequested = DateTime.Now,
            };

            return requestResponse;
        }
    }
}
