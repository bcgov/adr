namespace Adr.Semantics.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using Adr.Semantics.Models;
    using Adr.Semantics.Services;
    using Asp.Versioning;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Routing;

    /// <summary>
    /// Manages manually published glossary drafts.
    /// </summary>
    [ApiVersion("1.0")]
    [Route("v{version:apiVersion}/Glossary/drafts")]
    [ApiController]
    public sealed class GlossaryDraftController : ControllerBase
    {
        private readonly IGlossaryEditingService _editingService;

        /// <summary>
        /// Initializes a new instance of the <see cref="GlossaryDraftController"/> class.
        /// </summary>
        public GlossaryDraftController(IGlossaryEditingService editingService)
        {
            _editingService = editingService;
        }

        /// <summary>
        /// Creates a new draft based on the current glossary release.
        /// </summary>
        [HttpPost]
        [Produces("application/json")]
        [EndpointName("CreateGlossaryDraft")]
        [ProducesResponseType(typeof(BaseResponseModel<GlossaryDraftPreviewModel>), 201)]
        public ActionResult<BaseResponseModel<GlossaryDraftPreviewModel>> CreateDraft()
        {
            var response = CreateResponse(_editingService.CreateDraft());
            return CreatedAtAction(
                nameof(GetDraft),
                new { draftId = response.Payload.Draft.Id },
                response
            );
        }

        /// <summary>
        /// Returns glossary draft metadata, optionally filtered by lifecycle status.
        /// </summary>
        /// <param name="status">
        /// Optional case-insensitive lifecycle status: Draft, Published, or Stale.
        /// </param>
        [HttpGet]
        [Produces("application/json")]
        [EndpointName("GetGlossaryDrafts")]
        [ProducesResponseType(typeof(BaseResponseModel<IList<GlossaryDraftModel>>), 200)]
        public ActionResult<BaseResponseModel<IList<GlossaryDraftModel>>> GetDrafts(
            [FromQuery] string? status = null
        )
        {
            return Ok(CreateResponse(_editingService.GetDrafts(status)));
        }

        /// <summary>
        /// Returns a draft, its calculated draft version, and its effective terms.
        /// </summary>
        [HttpGet("{draftId:guid}")]
        [Produces("application/json")]
        [EndpointName("GetGlossaryDraft")]
        [ProducesResponseType(typeof(BaseResponseModel<GlossaryDraftPreviewModel>), 200)]
        [ProducesResponseType(404)]
        public ActionResult<BaseResponseModel<GlossaryDraftPreviewModel>> GetDraft(Guid draftId)
        {
            var draft = _editingService.GetDraft(draftId);
            return draft is null ? NotFound() : Ok(CreateResponse(draft));
        }

        /// <summary>
        /// Applies a stale draft's changes to the current glossary release.
        /// </summary>
        [HttpPost("{draftId:guid}/rebase")]
        [Produces("application/json")]
        [EndpointName("RebaseGlossaryDraft")]
        [ProducesResponseType(typeof(BaseResponseModel<GlossaryDraftRebaseResultModel>), 200)]
        [ProducesResponseType(typeof(BaseResponseModel<GlossaryDraftRebaseResultModel>), 409)]
        [ProducesResponseType(404)]
        public ActionResult<BaseResponseModel<GlossaryDraftRebaseResultModel>> RebaseDraft(
            Guid draftId
        )
        {
            try
            {
                var result = _editingService.Rebase(draftId);
                if (result is null)
                {
                    return NotFound();
                }

                var response = CreateResponse(result);
                return result.Status == "Rebased" ? Ok(response) : Conflict(response);
            }
            catch (GlossaryDraftConflictException exception)
            {
                return Conflict(exception.Message);
            }
        }

        /// <summary>
        /// Adds or replaces a term in a draft and records the submission for audit.
        /// </summary>
        [HttpPut("{draftId:guid}/terms/{term}")]
        [Produces("application/json")]
        [EndpointName("PutGlossaryDraftTerm")]
        [ProducesResponseType(typeof(BaseResponseModel<GlossaryTermSubmissionResultModel>), 200)]
        [ProducesResponseType(typeof(BaseResponseModel<GlossaryTermSubmissionResultModel>), 422)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public ActionResult<BaseResponseModel<GlossaryTermSubmissionResultModel>> PutTerm(
            Guid draftId,
            string term,
            [FromBody, Required] GlossaryTermEditModel edit
        )
        {
            try
            {
                var result = _editingService.PutTerm(draftId, term, edit);
                if (result is null)
                {
                    return NotFound();
                }

                var response = CreateResponse(result);
                return result.IsValid ? Ok(response) : UnprocessableEntity(response);
            }
            catch (GlossaryDraftConflictException exception)
            {
                return Conflict(exception.Message);
            }
        }

        /// <summary>
        /// Removes a term from a draft and records the deletion for audit.
        /// </summary>
        [HttpDelete("{draftId:guid}/terms/{term}")]
        [Produces("application/json")]
        [EndpointName("DeleteGlossaryDraftTerm")]
        [ProducesResponseType(typeof(BaseResponseModel<GlossaryTermSubmissionResultModel>), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(409)]
        public ActionResult<BaseResponseModel<GlossaryTermSubmissionResultModel>> DeleteTerm(
            Guid draftId,
            string term
        )
        {
            try
            {
                var result = _editingService.DeleteTerm(draftId, term);
                return result is null ? NotFound() : Ok(CreateResponse(result));
            }
            catch (GlossaryDraftConflictException exception)
            {
                return Conflict(exception.Message);
            }
        }

        private static BaseResponseModel<T> CreateResponse<T>(T payload)
        {
            return new BaseResponseModel<T>
            {
                Payload = payload,
                DatetimeRequested = DateTime.Now,
            };
        }
    }
}
