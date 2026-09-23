namespace Adr.Semantics.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using Adr.Semantics.Mappers;
    using Adr.Semantics.Models;
    using Adr.Semantics.Services;
    using Asp.Versioning;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Routing;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// The Glossary controller.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/[controller]")]
    [ApiController]
    public class GlossaryController : Controller
    {
        private const string CurrentSchemaCacheControl =
            "public, max-age=300, must-revalidate";

        private const string VersionedSchemaCacheControl =
            "public, max-age=31536000, immutable";

        private readonly ILogger<GlossaryController> _logger;

        private readonly IGlossaryService _glossaryService;

        private readonly IGlossaryEditingService _editingService;

        private readonly IMemoryCache _memoryCache;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlossaryController"/> class.
        /// </summary>
        /// <param name="logger">Injected Logger Provider.</param>
        /// <param name="glossaryService">Glossary service.</param>
        /// <param name="editingService">Glossary editing service.</param>
        /// <param name="memoryCache">In-memory schema document cache.</param>
        public GlossaryController(
            ILogger<GlossaryController> logger,
            IGlossaryService glossaryService,
            IGlossaryEditingService editingService,
            IMemoryCache memoryCache
        )
        {
            _logger = logger;
            _glossaryService = glossaryService;
            _editingService = editingService;
            _memoryCache = memoryCache;
        }

        /// <summary>
        /// Returns all glossary information.
        /// </summary>
        [HttpGet]
        [Produces("application/json")]
        [EndpointName("GetAllGlossary")]
        [ProducesResponseType(typeof(GlossaryResponseModel<IEnumerable<GlossaryModel>>), 200)]
        public GlossaryResponseModel<IEnumerable<GlossaryModel>> GetAll()
        {
            var glossaryInfo = _glossaryService.GetAll();
            var filteredGlossary = glossaryInfo.Where(g =>
                g.PublishToDevHub && g.VerifiedDefinitionFlag
            );
            var glossary = _glossaryService.GetCurrentVersion();
            SetVersionHeaders(glossary);
            return CreateResponse<IEnumerable<GlossaryModel>>(filteredGlossary, glossary);
        }

        /// <summary>
        /// Returns metadata for all available glossary releases.
        /// </summary>
        [HttpGet("versions")]
        [Produces("application/json")]
        [EndpointName("GetGlossaryVersions")]
        [ProducesResponseType(typeof(BaseResponseModel<IEnumerable<GlossaryVersionModel>>), 200)]
        public BaseResponseModel<IEnumerable<GlossaryVersionModel>> GetVersions()
        {
            return new BaseResponseModel<IEnumerable<GlossaryVersionModel>>
            {
                Payload = _glossaryService.GetVersions(),
                DatetimeRequested = DateTime.Now,
            };
        }

        /// <summary>
        /// Publishes a glossary draft as a new glossary version.
        /// </summary>
        [HttpPost("versions")]
        [Produces("application/json")]
        [EndpointName("PublishGlossaryVersion")]
        [ProducesResponseType(typeof(BaseResponseModel<GlossaryDraftPublishResultModel>), 201)]
        [ProducesResponseType(typeof(BaseResponseModel<GlossaryDraftPublishResultModel>), 409)]
        [ProducesResponseType(typeof(BaseResponseModel<GlossaryDraftPublishResultModel>), 422)]
        [ProducesResponseType(404)]
        public ActionResult<BaseResponseModel<GlossaryDraftPublishResultModel>> PublishVersion(
            [FromBody, Required] GlossaryVersionPublishModel request
        )
        {
            try
            {
                var result = _editingService.Publish(request.DraftId, request.IgnoreInvalid);
                if (result is null)
                {
                    return NotFound();
                }

                var response = new BaseResponseModel<GlossaryDraftPublishResultModel>
                {
                    Payload = result,
                    DatetimeRequested = DateTime.Now,
                };
                return result.Status switch
                {
                    "Published" => CreatedAtAction(
                        nameof(GetVersion),
                        new { glossaryVersion = result.Release!.Version },
                        response
                    ),
                    "Invalid" => UnprocessableEntity(response),
                    _ => Conflict(response),
                };
            }
            catch (GlossaryDraftConflictException exception)
            {
                return Conflict(exception.Message);
            }
        }

        /// <summary>
        /// Returns all published terms from a specific glossary release.
        /// </summary>
        /// <param name="glossaryVersion">The glossary semantic version.</param>
        [HttpGet("versions/{glossaryVersion}")]
        [Produces("application/json")]
        [EndpointName("GetGlossaryVersion")]
        [ProducesResponseType(typeof(GlossaryResponseModel<IEnumerable<GlossaryModel>>), 200)]
        [ProducesResponseType(404)]
        public ActionResult<GlossaryResponseModel<IEnumerable<GlossaryModel>>> GetVersion(
            string glossaryVersion
        )
        {
            var glossary = _glossaryService.GetVersion(glossaryVersion);
            var entries = _glossaryService.GetAll(glossaryVersion);
            if (glossary is null || entries is null)
            {
                return NotFound();
            }

            SetVersionHeaders(glossary);
            return Ok(
                CreateResponse<IEnumerable<GlossaryModel>>(
                    entries.Where(g => g.PublishToDevHub && g.VerifiedDefinitionFlag),
                    glossary
                )
            );
        }

        /// <summary>
        /// Returns the current glossary release as reusable OpenAPI schema components.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("schema")]
        [Produces("application/vnd.oai.openapi+json")]
        [EndpointName("GetGlossarySchema")]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        public JsonResult GetSchema()
        {
            var glossary = _glossaryService.GetCurrentVersion();
            var glossaryInfo = _glossaryService.GetAll();
            SetVersionHeaders(glossary);
            Response.Headers.CacheControl = CurrentSchemaCacheControl;
            return OpenApiContent(GetOpenApiDocument(glossaryInfo, glossary));
        }

        /// <summary>
        /// Returns a specific glossary release as reusable OpenAPI schema components.
        /// </summary>
        /// <param name="glossaryVersion">The glossary semantic version.</param>
        [AllowAnonymous]
        [HttpGet("versions/{glossaryVersion}/schema")]
        [Produces("application/vnd.oai.openapi+json")]
        [EndpointName("GetGlossaryVersionSchema")]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        [ProducesResponseType(404)]
        public ActionResult GetVersionSchema(string glossaryVersion)
        {
            var glossary = _glossaryService.GetVersion(glossaryVersion);
            var glossaryInfo = _glossaryService.GetAll(glossaryVersion);
            if (glossary is null || glossaryInfo is null)
            {
                return NotFound();
            }

            SetVersionHeaders(glossary);
            Response.Headers.CacheControl = VersionedSchemaCacheControl;
            return OpenApiContent(GetOpenApiDocument(glossaryInfo, glossary));
        }

        /// <summary>
        /// Returns all glossary information rendered as a Markdown table.
        /// </summary>
        [HttpGet("markdown")]
        [Produces("text/markdown")]
        [EndpointName("GetGlossaryMarkdown")]
        [ProducesResponseType(typeof(string), 200)]
        public ContentResult GetMarkdown()
        {
            var glossary = _glossaryService.GetCurrentVersion();
            var glossaryInfo = _glossaryService.GetAll();
            SetVersionHeaders(glossary);
            var markdown = GlossaryMarkdownMapper.Map(glossaryInfo, glossary);
            return Content(markdown, "text/markdown; charset=utf-8");
        }

        /// <summary>
        /// Returns a specific glossary release rendered as a Markdown table.
        /// </summary>
        /// <param name="glossaryVersion">The glossary semantic version.</param>
        [HttpGet("versions/{glossaryVersion}/markdown")]
        [Produces("text/markdown")]
        [EndpointName("GetGlossaryVersionMarkdown")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        public ActionResult GetVersionMarkdown(string glossaryVersion)
        {
            var glossary = _glossaryService.GetVersion(glossaryVersion);
            var glossaryInfo = _glossaryService.GetAll(glossaryVersion);
            if (glossary is null || glossaryInfo is null)
            {
                return NotFound();
            }

            SetVersionHeaders(glossary);
            return Content(
                GlossaryMarkdownMapper.Map(glossaryInfo, glossary),
                "text/markdown; charset=utf-8"
            );
        }

        /// <summary>
        /// Returns all glossary information rendered as a Markdown list.
        /// </summary>
        [HttpGet("markdown-list")]
        [Produces("text/markdown")]
        [EndpointName("GetGlossaryMarkdownList")]
        [ProducesResponseType(typeof(string), 200)]
        public ContentResult GetMarkdownList()
        {
            var glossary = _glossaryService.GetCurrentVersion();
            var glossaryInfo = _glossaryService.GetAll();
            SetVersionHeaders(glossary);
            var markdown = GlossaryMarkdownListMapper.Map(glossaryInfo, glossary);
            return Content(markdown, "text/markdown; charset=utf-8");
        }

        /// <summary>
        /// Returns a specific glossary release rendered as a Markdown list.
        /// </summary>
        /// <param name="glossaryVersion">The glossary semantic version.</param>
        [HttpGet("versions/{glossaryVersion}/markdown-list")]
        [Produces("text/markdown")]
        [EndpointName("GetGlossaryVersionMarkdownList")]
        [ProducesResponseType(typeof(string), 200)]
        [ProducesResponseType(404)]
        public ActionResult GetVersionMarkdownList(string glossaryVersion)
        {
            var glossary = _glossaryService.GetVersion(glossaryVersion);
            var glossaryInfo = _glossaryService.GetAll(glossaryVersion);
            if (glossary is null || glossaryInfo is null)
            {
                return NotFound();
            }

            SetVersionHeaders(glossary);
            return Content(
                GlossaryMarkdownListMapper.Map(glossaryInfo, glossary),
                "text/markdown; charset=utf-8"
            );
        }

        /// <summary>
        /// Returns every persisted version of a glossary term.
        /// </summary>
        /// <param name="term">The current human-readable term slug.</param>
        [HttpGet("terms/{term}/versions")]
        [Produces("application/json")]
        [EndpointName("GetGlossaryTermVersions")]
        [ProducesResponseType(typeof(BaseResponseModel<IEnumerable<GlossaryModel>>), 200)]
        [ProducesResponseType(404)]
        public ActionResult<BaseResponseModel<IEnumerable<GlossaryModel>>> GetTermVersions(
            string term
        )
        {
            var versions = _glossaryService.GetTermVersions(term);
            if (versions is null)
            {
                return NotFound();
            }

            return Ok(
                new BaseResponseModel<IEnumerable<GlossaryModel>>
                {
                    Payload = versions,
                    DatetimeRequested = DateTime.Now,
                }
            );
        }

        /// <summary>
        /// Returns the published revisions and invalid submissions for a term selected from a
        /// specific glossary release.
        /// </summary>
        /// <param name="glossaryVersion">The glossary release containing the term.</param>
        /// <param name="term">The term slug as it appears in that release.</param>
        [HttpGet("versions/{glossaryVersion}/terms/{term}/versions")]
        [Produces("application/json")]
        [EndpointName("GetGlossaryTermHistory")]
        [ProducesResponseType(
            typeof(BaseResponseModel<IEnumerable<GlossaryTermHistoryModel>>),
            200
        )]
        [ProducesResponseType(404)]
        public ActionResult<
            BaseResponseModel<IEnumerable<GlossaryTermHistoryModel>>
        > GetTermVersions(string glossaryVersion, string term)
        {
            var history = _glossaryService.GetTermHistory(glossaryVersion, term);
            if (history is null)
            {
                return NotFound();
            }

            return Ok(
                new BaseResponseModel<IEnumerable<GlossaryTermHistoryModel>>
                {
                    Payload = history,
                    DatetimeRequested = DateTime.Now,
                }
            );
        }

        /// <summary>
        /// Returns a persisted version of a glossary term.
        /// </summary>
        /// <param name="term">The current human-readable term slug.</param>
        /// <param name="termVersion">The monotonically increasing term version.</param>
        [HttpGet("terms/{term}/versions/{termVersion:int:min(1)}")]
        [Produces("application/json")]
        [EndpointName("GetGlossaryTermVersion")]
        [ProducesResponseType(typeof(BaseResponseModel<GlossaryModel>), 200)]
        [ProducesResponseType(404)]
        public ActionResult<BaseResponseModel<GlossaryModel>> GetTermVersion(
            string term,
            int termVersion
        )
        {
            var version = _glossaryService.GetTermVersion(term, termVersion);
            if (version is null)
            {
                return NotFound();
            }

            return Ok(
                new BaseResponseModel<GlossaryModel>
                {
                    Payload = version,
                    DatetimeRequested = DateTime.Now,
                }
            );
        }

        /// <summary>
        /// Returns a persisted term revision as a reusable OpenAPI Schema Object.
        /// </summary>
        /// <param name="term">The current human-readable term slug.</param>
        /// <param name="termVersion">The monotonically increasing term version.</param>
        [AllowAnonymous]
        [HttpGet("terms/{term}/versions/{termVersion:int:min(1)}/schema")]
        [Produces("application/json")]
        [EndpointName("GetGlossaryTermVersionSchema")]
        [ProducesResponseType(typeof(Dictionary<string, object>), 200)]
        [ProducesResponseType(404)]
        public ActionResult GetTermVersionSchema(string term, int termVersion)
        {
            var entry = _glossaryService.GetTermVersion(term, termVersion);
            var glossary = _glossaryService.GetCurrentVersion();
            if (
                entry is null
                || !entry.PublishToDevHub
                || !entry.VerifiedDefinitionFlag
            )
            {
                return NotFound();
            }

            Response.Headers.ETag =
                $"\"{glossary.Id}:{entry.Name}:{entry.Version}\"";
            Response.Headers.CacheControl = VersionedSchemaCacheControl;
            return new JsonResult(GetOpenApiTermSchema(entry, glossary.Id));
        }

        /// <summary>
        /// Returns the current valid glossary entry identified by its stable UUID.
        /// </summary>
        /// <param name="id">The stable term UUID.</param>
        [HttpGet("id/{id:guid}")]
        [Produces("application/json")]
        [EndpointName("GetGlossaryEntryById")]
        [ProducesResponseType(typeof(GlossaryResponseModel<GlossaryModel>), 200)]
        [ProducesResponseType(404)]
        public ActionResult<GlossaryResponseModel<GlossaryModel>> GetById(Guid id)
        {
            var entry = _glossaryService.GetGlossaryEntryById(id);
            if (entry is null)
            {
                return NotFound();
            }

            var glossary = _glossaryService.GetCurrentVersion();
            SetVersionHeaders(glossary);
            return Ok(CreateResponse(entry, glossary));
        }

        /// <summary>
        /// Returns a glossary entry by its human-readable slug.
        /// </summary>
        /// <param name="term">The glossary term slug.</param>
        [HttpGet("{term}")]
        [Produces("application/json")]
        [EndpointName("GetGlossaryEntryByTerm")]
        [ProducesResponseType(typeof(GlossaryResponseModel<GlossaryModel>), 200)]
        [ProducesResponseType(404)]
        public ActionResult<GlossaryResponseModel<GlossaryModel>> GetByTerm(string term)
        {
            var publicBody = _glossaryService.GetGlossaryEntryByTerm(term);
            if (publicBody == null)
            {
                return NotFound();
            }

            var glossary = _glossaryService.GetCurrentVersion();
            SetVersionHeaders(glossary);
            return Ok(CreateResponse(publicBody, glossary));
        }

        /// <summary>
        /// Returns a glossary entry from a specific release by its human-readable slug.
        /// </summary>
        /// <param name="glossaryVersion">The glossary semantic version.</param>
        /// <param name="term">The glossary term slug.</param>
        [HttpGet("versions/{glossaryVersion}/terms/{term}")]
        [Produces("application/json")]
        [EndpointName("GetGlossaryVersionEntryByTerm")]
        [ProducesResponseType(typeof(GlossaryResponseModel<GlossaryModel>), 200)]
        [ProducesResponseType(404)]
        public ActionResult<GlossaryResponseModel<GlossaryModel>> GetVersionByTerm(
            string glossaryVersion,
            string term
        )
        {
            var glossary = _glossaryService.GetVersion(glossaryVersion);
            var entry = _glossaryService.GetGlossaryEntryByTerm(term, glossaryVersion);
            if (glossary is null || entry is null)
            {
                return NotFound();
            }

            SetVersionHeaders(glossary);
            return Ok(CreateResponse(entry, glossary));
        }

        private static GlossaryResponseModel<T> CreateResponse<T>(
            T payload,
            GlossaryVersionModel glossary
        )
        {
            return new GlossaryResponseModel<T>
            {
                Glossary = glossary,
                Payload = payload,
                DatetimeRequested = DateTime.Now,
            };
        }

        private void SetVersionHeaders(GlossaryVersionModel glossary)
        {
            Response.Headers.ETag = $"\"{glossary.Id}:{glossary.Version}\"";
        }

        private Dictionary<string, object> GetOpenApiDocument(
            IEnumerable<GlossaryModel> glossaryInfo,
            GlossaryVersionModel glossary
        )
        {
            var cacheKey = $"glossary-openapi:{glossary.Id}:{glossary.Version}";
            return _memoryCache.GetOrCreate(
                    cacheKey,
                    _ => GlossaryOpenApiMapper.Map(glossaryInfo, glossary)
                )
                ?? throw new InvalidOperationException(
                    $"Could not generate the OpenAPI document for glossary version '{glossary.Version}'."
                );
        }

        private Dictionary<string, object> GetOpenApiTermSchema(
            GlossaryModel entry,
            string glossaryId
        )
        {
            var cacheKey =
                $"glossary-openapi-term:{glossaryId}:{entry.Name}:{entry.Version}";
            return _memoryCache.GetOrCreate(
                    cacheKey,
                    _ => GlossaryOpenApiMapper.MapSchema(entry, glossaryId)
                )
                ?? throw new InvalidOperationException(
                    $"Could not generate the OpenAPI schema for term '{entry.Name}' version '{entry.Version}'."
                );
        }

        private static JsonResult OpenApiContent(Dictionary<string, object> document)
        {
            return new JsonResult(document)
            {
                ContentType = "application/vnd.oai.openapi+json;version=3.0",
            };
        }
    }
}
