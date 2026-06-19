namespace Adr.PublicBodies.Models
{
  /// <summary>
  /// Response payload returned to the front end for CHEFS auth token requests.
  /// </summary>
  public class ChefsTokenResponseModel
  {
    /// <summary>
    /// Gets or sets the CHEFS form identifier.
    /// </summary>
    public string FormId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the short-lived CHEFS gateway token.
    /// </summary>
    public string AuthToken { get; set; } = string.Empty;
  }
}
