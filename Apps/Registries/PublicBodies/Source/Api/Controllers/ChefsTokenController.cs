namespace Adr.PublicBodies.Controllers
{
  using System;
  using System.Collections.Generic;
  using System.Net.Http;
  using System.Threading;
  using System.Threading.Tasks;
  using Adr.PublicBodies.Models;
  using Adr.PublicBodies.Services;
  using Microsoft.AspNetCore.Http;
  using Microsoft.AspNetCore.Mvc;
  using Microsoft.Extensions.Logging;

  /// <summary>
  /// Issues short-lived CHEFS gateway tokens to the frontend.
  /// </summary>
  [ApiController]
  [Route("api/chefs-token")]
  public class ChefsTokenController : ControllerBase
  {
    private readonly ILogger<ChefsTokenController> _logger;
    private readonly IChefsTokenService _chefsTokenService;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChefsTokenController"/> class.
    /// </summary>
    /// <param name="logger">The injected logger.</param>
    /// <param name="chefsTokenService">The CHEFS token exchange service.</param>
    public ChefsTokenController(
        ILogger<ChefsTokenController> logger,
        IChefsTokenService chefsTokenService
    )
    {
      _logger = logger;
      _chefsTokenService = chefsTokenService;
    }

    /// <summary>
    /// Returns a short-lived CHEFS gateway token for a configured form.
    /// </summary>
    /// <param name="formId">The CHEFS form identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    [HttpGet("{formId}")]
    [Produces("application/json")]
    [ProducesResponseType(typeof(ChefsTokenResponseModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status502BadGateway)]
    public async Task<ActionResult<ChefsTokenResponseModel>> Get(
        string formId,
        CancellationToken cancellationToken
    )
    {
      try
      {
        ChefsTokenResponseModel response = await _chefsTokenService.GetTokenAsync(
            formId,
            cancellationToken
        );

        return Ok(response);
      }
      catch (KeyNotFoundException exception)
      {
        _logger.LogWarning(exception, "CHEFS token requested for unknown form {FormId}", formId);
        return NotFound();
      }
      catch (HttpRequestException exception)
      {
        _logger.LogError(
            exception,
            "CHEFS token exchange failed for form {FormId}",
            formId
        );
        return Problem(
            statusCode: StatusCodes.Status502BadGateway,
            title: "CHEFS token exchange failed."
        );
      }
      catch (ArgumentException exception)
      {
        _logger.LogWarning(exception, "CHEFS token requested with invalid form ID");
        return BadRequest();
      }
      catch (InvalidOperationException exception)
      {
        _logger.LogError(exception, "CHEFS integration is not configured correctly.");
        return Problem(
            statusCode: StatusCodes.Status500InternalServerError,
            title: "CHEFS integration is not configured correctly."
        );
      }
    }
  }
}
