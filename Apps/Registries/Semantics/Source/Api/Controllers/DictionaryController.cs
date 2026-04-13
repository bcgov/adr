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
    /// The Dictionary controller.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    [ApiController]
    public class DictionaryController : Controller
    {
        private readonly ILogger<DictionaryController> _logger;

        private readonly IDictionaryService _dictionaryService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DictionaryController"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        /// <param name="ministryService">Dictionary service.</param>
        public DictionaryController(
            ILogger<DictionaryController> logger,
            IDictionaryService dictionaryService
        )
        {
            _logger = logger;
            _dictionaryService = dictionaryService;
        }

        /// <summary>
        /// Returns all dictionary information.
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        [EndpointName("GetAllDictionaries")]
        [ProducesResponseType(typeof(BaseResponseModel<IEnumerable<DictionaryModel>>), 200)]
        public BaseResponseModel<IEnumerable<DictionaryModel>> GetAll()
        {
            var dictionaryInfo = _dictionaryService.GetAll();
            var requestResponse = new BaseResponseModel<IEnumerable<DictionaryModel>>()
            {
                Payload = dictionaryInfo,
                DatetimeRequested = DateTime.Now,
            };

            return requestResponse;
        }
    }
}
