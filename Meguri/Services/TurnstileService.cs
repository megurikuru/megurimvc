using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Meguri.Services {
    public interface ITurnstileService {
        Task<bool> VerifyAsync(string? token, string expectedAction, string? remoteIp = null, CancellationToken cancellationToken = default);
    }

    public class TurnstileService : ITurnstileService {
        private const string SiteVerifyUrl = "https://challenges.cloudflare.com/turnstile/v0/siteverify";
        private readonly HttpClient _httpClient;
        private readonly TurnstileConf _conf;
        private readonly ILogger<TurnstileService> _logger;

        public TurnstileService(
            HttpClient httpClient,
            IOptions<TurnstileConf> options,
            ILogger<TurnstileService> logger) {
            _httpClient = httpClient;
            _conf = options.Value;
            _logger = logger;
        }

        public async Task<bool> VerifyAsync(string? token, string expectedAction, string? remoteIp = null, CancellationToken cancellationToken = default) {
            var expectedHostnames = new HashSet<string>(
                (_conf.Hostnames ?? Array.Empty<string>())
                    .Select(h => h.Trim())
                    .Where(h => !string.IsNullOrEmpty(h)),
                StringComparer.OrdinalIgnoreCase);

            if (string.IsNullOrEmpty(token) || token.Length > 2048 || expectedHostnames.Count == 0 || string.IsNullOrWhiteSpace(_conf.SecretKey)) {
                return false;
            }

            try {
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(10));

                var formValues = new Dictionary<string, string> {
                    ["secret"] = _conf.SecretKey,
                    ["response"] = token
                };
                if (!string.IsNullOrWhiteSpace(remoteIp)) {
                    formValues["remoteip"] = remoteIp;
                }

                using var content = new FormUrlEncodedContent(formValues);
                using var response = await _httpClient.PostAsync(SiteVerifyUrl, content, cts.Token);
                if (!response.IsSuccessStatusCode) {
                    _logger.LogWarning("Turnstile siteverify returned non-success status code: {StatusCode}", (int)response.StatusCode);
                    return false;
                }

                var result = await response.Content.ReadFromJsonAsync<SiteVerifyResponse>(cancellationToken: cts.Token);
                if (result == null) {
                    return false;
                }

                if (!result.Success ||
                    !string.Equals(result.Action, expectedAction, StringComparison.Ordinal) ||
                    string.IsNullOrEmpty(result.Hostname) ||
                    !expectedHostnames.Contains(result.Hostname)) {
                    _logger.LogWarning(
                        "Turnstile verification failed. Success={Success}, Action={Action}, ExpectedAction={ExpectedAction}, Hostname={Hostname}",
                        result.Success, result.Action, expectedAction, result.Hostname);
                    return false;
                }

                return true;
            } catch (Exception ex) {
                _logger.LogWarning(ex, "Turnstile verification encountered an exception.");
                return false;
            }
        }

        private sealed class SiteVerifyResponse {
            [JsonPropertyName("success")]
            public bool Success { get; set; }

            [JsonPropertyName("action")]
            public string? Action { get; set; }

            [JsonPropertyName("hostname")]
            public string? Hostname { get; set; }

            [JsonPropertyName("error-codes")]
            public string[]? ErrorCodes { get; set; }
        }
    }
}
