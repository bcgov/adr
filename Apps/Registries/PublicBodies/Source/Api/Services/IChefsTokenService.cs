namespace Adr.PublicBodies.Services
{
  using System.Threading;
  using System.Threading.Tasks;
  using Adr.PublicBodies.Models;

  /// <summary>
  /// Exchanges a configured CHEFS API key for a short-lived gateway token.
  /// </summary>
  public interface IChefsTokenService
  {
    /// <summary>
    /// Fetches a CHEFS gateway token for the requested form.
    /// </summary>
    /// <param name="formId">The CHEFS form identifier.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The token payload consumed by the frontend web component.</returns>
    Task<ChefsTokenResponseModel> GetTokenAsync(
        string formId,
        CancellationToken cancellationToken = default
    );
  }
}
