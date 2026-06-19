namespace Adr.PublicBodies.Configuration.Models
{
  using System.Collections.Generic;

  /// <summary>
  /// Configuration for CHEFS integration.
  /// </summary>
  public class ChefsConfiguration
  {
    /// <summary>
    /// Gets or sets the CHEFS base URL.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the allowed CHEFS forms.
    /// </summary>
    public List<ChefsFormConfiguration> Forms { get; set; } = [];
  }

  /// <summary>
  /// Configuration for a single CHEFS form.
  /// </summary>
  public class ChefsFormConfiguration
  {
    /// <summary>
    /// Gets or sets the CHEFS form identifier.
    /// </summary>
    public string FormId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the CHEFS API key used to mint gateway tokens.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
  }
}
