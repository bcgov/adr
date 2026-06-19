namespace Adr.PublicBodies.Services
{
  using System;
  using System.Collections.Generic;
  using System.Diagnostics;
  using System.Linq;
  using System.Net;
  using System.Net.Http;
  using System.Net.Http.Json;
  using System.Security.Cryptography;
  using System.Text;
  using System.Text.Json;
  using System.Threading;
  using System.Threading.Tasks;
  using Adr.PublicBodies.Configuration.Models;
  using Adr.PublicBodies.Models;
  using Microsoft.Extensions.Logging;
  using Microsoft.Extensions.Options;

  /// <summary>
  /// Server-side integration for minting CHEFS gateway tokens.
  /// </summary>
  public class ChefsTokenService : IChefsTokenService
  {
    private static readonly JsonSerializerOptions _jsonSerializerOptions =
        new() { PropertyNameCaseInsensitive = true };
    private static readonly HttpClient _chefsHttpClient = new(
        new SocketsHttpHandler
        {
          AllowAutoRedirect = false,
          AutomaticDecompression = DecompressionMethods.None,
          ActivityHeadersPropagator = DistributedContextPropagator.CreateNoOutputPropagator(),
          UseCookies = false,
          UseProxy = false,
        }
    );

    private readonly ILogger<ChefsTokenService> _logger;
    private readonly ChefsConfiguration _configuration;

    /// <summary>
    /// Initializes a new instance of the <see cref="ChefsTokenService"/> class.
    /// </summary>
    /// <param name="logger">The injected logger.</param>
    /// <param name="options">The bound CHEFS configuration.</param>
    public ChefsTokenService(
        ILogger<ChefsTokenService> logger,
        IOptions<ChefsConfiguration> options
    )
    {
      _logger = logger;
      _configuration = options.Value;
    }

    /// <inheritdoc />
    public async Task<ChefsTokenResponseModel> GetTokenAsync(
        string formId,
        CancellationToken cancellationToken = default
    )
    {
      if (string.IsNullOrWhiteSpace(formId))
      {
        throw new ArgumentException("A CHEFS form ID is required.", nameof(formId));
      }

      string normalizedFormId = formId.Trim();
      string normalizedBaseUrl = _configuration.BaseUrl.Trim();

      ChefsFormConfiguration? form = _configuration.Forms.FirstOrDefault(config =>
        string.Equals(config.FormId?.Trim(), normalizedFormId, StringComparison.OrdinalIgnoreCase)
    );

      if (form == null)
      {
        throw new KeyNotFoundException($"CHEFS form '{formId}' is not configured.");
      }

      if (string.IsNullOrWhiteSpace(normalizedBaseUrl))
      {
        throw new InvalidOperationException("CHEFS base URL is not configured.");
      }

      string configuredFormId = form.FormId.Trim();
      string configuredApiKey = form.ApiKey.Trim();

      if (string.IsNullOrWhiteSpace(configuredApiKey))
      {
        throw new InvalidOperationException(
            $"CHEFS API key for form '{normalizedFormId}' is not configured."
        );
      }

      string authValue = Convert.ToBase64String(
        Encoding.ASCII.GetBytes($"{configuredFormId}:{configuredApiKey}")
      );

      string requestUri =
        $"{normalizedBaseUrl.TrimEnd('/')}/gateway/v1/auth/token/forms/{configuredFormId}";
      string requestBody = $"{{\"formId\":\"{configuredFormId}\"}}";

      _logger.LogInformation(
          "CHEFS token request details: Uri={RequestUri}, FormId={FormId}, ApiKeyLength={ApiKeyLength}, AuthFingerprint={AuthFingerprint}, Body={RequestBody}",
          requestUri,
          configuredFormId,
          configuredApiKey.Length,
          GetSha256Fingerprint(authValue),
          requestBody
      );

      using HttpRequestMessage request = new(
          HttpMethod.Post,
          requestUri
      );
      request.Headers.TryAddWithoutValidation("Authorization", $"Basic {authValue}");
      request.Content = new StringContent(
        requestBody,
        Encoding.UTF8,
        "application/json"
      );

      using HttpResponseMessage response = await _chefsHttpClient.SendAsync(
        request,
        cancellationToken
    );

      if (!response.IsSuccessStatusCode)
      {
        string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
          _logger.LogWarning(
              "CHEFS rejected the in-process HttpClient request for form {FormId}. Trying curl fallback.",
              configuredFormId
          );

          string? curlToken = await TryGetTokenWithCurlAsync(
              requestUri,
              configuredFormId,
              configuredApiKey,
              requestBody,
              cancellationToken
          );

          if (!string.IsNullOrWhiteSpace(curlToken))
          {
            return new ChefsTokenResponseModel
            {
              FormId = configuredFormId,
              AuthToken = curlToken,
            };
          }
        }

        _logger.LogError(
            "CHEFS token request failed for form {FormId} with status {StatusCode}. Response: {ResponseBody}",
            configuredFormId,
            (int)response.StatusCode,
            responseBody
        );
        throw new HttpRequestException(
            $"CHEFS token request failed with status code {(int)response.StatusCode}.",
            null,
            response.StatusCode
        );
      }

      ChefsGatewayTokenResponse? payload =
          await response.Content.ReadFromJsonAsync<ChefsGatewayTokenResponse>(
              _jsonSerializerOptions,
              cancellationToken
          );

      if (payload == null || string.IsNullOrWhiteSpace(payload.Token))
      {
        throw new InvalidOperationException("CHEFS token response did not include a token.");
      }

      return new ChefsTokenResponseModel { FormId = configuredFormId, AuthToken = payload.Token };
    }

    private sealed class ChefsGatewayTokenResponse
    {
      public string Token { get; set; } = string.Empty;
    }

    private static string GetSha256Fingerprint(string value)
    {
      byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(value));
      return Convert.ToHexString(hash[..8]);
    }

    private async Task<string?> TryGetTokenWithCurlAsync(
        string requestUri,
        string formId,
        string apiKey,
        string requestBody,
        CancellationToken cancellationToken
    )
    {
      using Process process = new();
      process.StartInfo = new ProcessStartInfo
      {
        FileName = "curl",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
      };

      process.StartInfo.ArgumentList.Add("-sS");
      process.StartInfo.ArgumentList.Add("-X");
      process.StartInfo.ArgumentList.Add("POST");
      process.StartInfo.ArgumentList.Add(requestUri);
      process.StartInfo.ArgumentList.Add("-u");
      process.StartInfo.ArgumentList.Add($"{formId}:{apiKey}");
      process.StartInfo.ArgumentList.Add("-H");
      process.StartInfo.ArgumentList.Add("Content-Type: application/json");
      process.StartInfo.ArgumentList.Add("--data");
      process.StartInfo.ArgumentList.Add(requestBody);
      process.StartInfo.ArgumentList.Add("-o");
      process.StartInfo.ArgumentList.Add("-");
      process.StartInfo.ArgumentList.Add("-w");
      process.StartInfo.ArgumentList.Add("\n%{http_code}");

      process.Start();

      string standardOutput = await process.StandardOutput.ReadToEndAsync(cancellationToken);
      string standardError = await process.StandardError.ReadToEndAsync(cancellationToken);
      await process.WaitForExitAsync(cancellationToken);

      if (process.ExitCode != 0)
      {
        _logger.LogError(
            "curl fallback failed for form {FormId}. ExitCode={ExitCode}, Error={Error}",
            formId,
            process.ExitCode,
            standardError
        );
        return null;
      }

      string[] outputParts = standardOutput.TrimEnd().Split('\n');
      if (outputParts.Length < 2)
      {
        _logger.LogError(
            "curl fallback returned an unexpected response shape for form {FormId}.",
            formId
        );
        return null;
      }

      string statusCodeText = outputParts[^1];
      string responseJson = string.Join('\n', outputParts[..^1]);

      if (!string.Equals(statusCodeText, "201", StringComparison.Ordinal))
      {
        _logger.LogError(
            "curl fallback returned status {StatusCode} for form {FormId}. Response: {Response}",
            statusCodeText,
            formId,
            responseJson
        );
        return null;
      }

      ChefsGatewayTokenResponse? payload = JsonSerializer.Deserialize<ChefsGatewayTokenResponse>(
          responseJson,
          _jsonSerializerOptions
      );

      if (payload == null || string.IsNullOrWhiteSpace(payload.Token))
      {
        _logger.LogError(
            "curl fallback did not return a CHEFS token for form {FormId}.",
            formId
        );
        return null;
      }

      _logger.LogInformation("curl fallback succeeded for CHEFS form {FormId}.", formId);
      return payload.Token;
    }
  }
}
