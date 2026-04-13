namespace Adr.Semantics.Controllers
{
    using System;
    using System.Collections.Generic;
    using Adr.Semantics.Models;
    using Adr.Semantics.Services;
    using Asp.Versioning;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The Glossary controller.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    [ApiController]
    public class GlossaryController : Controller
    {
        private readonly ILogger<GlossaryController> _logger;

        private readonly IGlossaryService _glossaryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlossaryController"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        /// <param name="ministryService">Glossary service.</param>
        public GlossaryController(
            ILogger<GlossaryController> logger,
            IGlossaryService glossaryService
        )
        {
            _logger = logger;
            _glossaryService = glossaryService;
        }

        /// <summary>
        /// Returns all glossary information.
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        [EndpointName("GetAllGlossary")]
        [ProducesResponseType(typeof(BaseResponseModel<IEnumerable<GlossaryModel>>), 200)]
        public BaseResponseModel<IEnumerable<GlossaryModel>> GetAll()
        {
            var glossaryInfo = _glossaryService.GetAll();
            var requestResponse = new BaseResponseModel<IEnumerable<GlossaryModel>>()
            {
                Payload = glossaryInfo,
                DatetimeRequested = DateTime.Now,
            };

            return requestResponse;
        }

        /// <summary>
        /// Returns a public body by its static ID.
        /// </summary>
        /// <param name="id">The Public Body Static id</param>
        [HttpGet("{term}")]
        [Produces("application/json")]
        [EndpointName("GetGlossaryEntryByTerm")]
        [ProducesResponseType(typeof(BaseResponseModel<GlossaryModel>), 200)]
        [ProducesResponseType(404)]
        public ActionResult<BaseResponseModel<GlossaryModel>> GetByTerm(string term)
        {
            var publicBody = _glossaryService.GetGlossaryEntryByTerm(term);
            if (publicBody == null)
            {
                return NotFound();
            }

            var requestResponse = new BaseResponseModel<GlossaryModel>()
            {
                Payload = publicBody,
                DatetimeRequested = DateTime.Now,
            };

            return Ok(requestResponse);
        }
    }
}
