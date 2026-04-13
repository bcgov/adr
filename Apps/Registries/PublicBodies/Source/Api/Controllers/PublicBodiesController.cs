namespace Adr.PublicBodies.Controllers
{
    using System;
    using System.Collections.Generic;
    using Adr.PublicBodies.Models;
    using Adr.PublicBodies.Services;
    using Asp.Versioning;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The PublicBodies controller.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
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

        /// <summary>
        /// Returns all public bodies.
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        [EndpointName("GetAllPublicBodies")]
        [ProducesResponseType(typeof(BaseResponseModel<IEnumerable<PublicBodyModel>>), 200)]
        public BaseResponseModel<IEnumerable<PublicBodyModel>> GetAllPublicBodies()
        {
            var publicBodies = _publicBodyService.GetAll();
            var requestResponse = new BaseResponseModel<IEnumerable<PublicBodyModel>>()
            {
                Payload = publicBodies,
                DatetimeRequested = DateTime.Now,
            };

            return requestResponse;
        }

        /// <summary>
        /// Returns a public body by its static ID.
        /// </summary>
        /// <param name="id">The Public Body Static id</param>
        [HttpGet("{id}")]
        [Produces("application/json")]
        [EndpointName("GetPublicBodyById")]
        [ProducesResponseType(typeof(BaseResponseModel<PublicBodyModel>), 200)]
        [ProducesResponseType(404)]
        public ActionResult<BaseResponseModel<PublicBodyModel>> GetById(string id)
        {
            var publicBody = _publicBodyService.GetPublicBody(id);
            if (publicBody == null)
            {
                return NotFound();
            }

            var requestResponse = new BaseResponseModel<PublicBodyModel>()
            {
                Payload = publicBody,
                DatetimeRequested = DateTime.Now,
            };

            return Ok(requestResponse);
        }

        /// <summary>
        /// Returns all public body types.
        /// </summary>
        [HttpGet("types")]
        [Produces("application/json")]
        [EndpointName("GetPublicBodyTypes")]
        [ProducesResponseType(typeof(BaseResponseModel<IEnumerable<PublicBodyTypeModel>>), 200)]
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

        /// <summary>
        /// Returns all parent-child relationships between public bodies.
        /// </summary>
        [HttpGet("relationships")]
        [Produces("application/json")]
        [EndpointName("GetPublicBodyRelationships")]
        [ProducesResponseType(
            typeof(BaseResponseModel<IEnumerable<PublicBodyParentChildModel>>),
            200
        )]
        public BaseResponseModel<
            IEnumerable<PublicBodyParentChildModel>
        > GetParentChildRelationships()
        {
            var relationships = _publicBodyService.GetAllParentChildRelationships();
            var requestResponse = new BaseResponseModel<IEnumerable<PublicBodyParentChildModel>>()
            {
                Payload = relationships,
                DatetimeRequested = DateTime.Now,
            };

            return requestResponse;
        }

        /// <summary>
        /// Returns the full lineage history for a public body as a directed acyclic graph.
        /// </summary>
        /// <param name="id">The Public Body Static id</param>
        [HttpGet("{id}/history")]
        [Produces("application/json")]
        [EndpointName("GetPublicBodyHistory")]
        [ProducesResponseType(typeof(BaseResponseModel<PublicBodyHistoryModel>), 200)]
        [ProducesResponseType(404)]
        public ActionResult<BaseResponseModel<PublicBodyHistoryModel>> GetHistory(string id)
        {
            var history = _publicBodyService.GetHistory(id);
            if (history == null)
            {
                return NotFound();
            }

            var requestResponse = new BaseResponseModel<PublicBodyHistoryModel>()
            {
                Payload = history,
                DatetimeRequested = DateTime.Now,
            };

            return Ok(requestResponse);
        }
    }
}
