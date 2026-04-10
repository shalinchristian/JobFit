using System.Net.Http.Json;
using ResumeAPI.DTOs;

namespace ResumeAPI.Services;

// ──────────────────────────────────────────────
// Interface
// ──────────────────────────────────────────────

public interface ISimilarityClient
{
    /// <summary>Calls the Python microservice and returns a similarity score (0–100).</summary>
    Task<double> GetSimilarityScoreAsync(string text1, string text2);
}

// ──────────────────────────────────────────────
// Implementation
// ──────────────────────────────────────────────

/// <summary>
/// HTTP client wrapper that communicates with the Flask/FastAPI Python microservice.
/// Uses IHttpClientFactory for efficient connection reuse and testability.
/// </summary>
public class SimilarityClient : ISimilarityClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SimilarityClient> _logger;

    // Named HttpClient registered in Program.cs as "PythonService"
    public SimilarityClient(HttpClient httpClient, ILogger<SimilarityClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// POSTs both texts to the Python /similarity endpoint and returns the score.
    /// Throws HttpRequestException if the Python service is unreachable.
    /// </summary>
    public async Task<double> GetSimilarityScoreAsync(string text1, string text2)
    {
        var payload = new SimilarityRequestDto { Text1 = text1, Text2 = text2 };

        _logger.LogInformation("Calling Python similarity service...");

        // Send POST request with JSON body
        var response = await _httpClient.PostAsJsonAsync("/similarity", payload);

        // Throw if HTTP status is not 2xx
        response.EnsureSuccessStatusCode();

        // Deserialize the JSON response body
        var result = await response.Content.ReadFromJsonAsync<SimilarityResponseDto>();

        if (result == null)
            throw new InvalidOperationException("Python service returned an empty response.");

        _logger.LogInformation("Similarity score received: {Score}", result.Score);
        return result.Score;
    }
}
