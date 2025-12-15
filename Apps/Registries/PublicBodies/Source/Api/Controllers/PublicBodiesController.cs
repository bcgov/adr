namespace Adr.PublicBodies.Controllers
{
    using Adr.PublicBodies.Models;
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

        /// <summary>
        /// Initializes a new instance of the <see cref="PublicBodiesController"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        public PublicBodiesController(ILogger<PublicBodiesController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        [Produces("application/json")]
        public BaseResponseModel Index()
        {
            var a = new BaseResponseModel();
            a.DummyVariable = "Hello Adr World";
            return a;
        }
    }
}
