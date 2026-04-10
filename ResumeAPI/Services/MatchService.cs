using ResumeAPI.DTOs;
using ResumeAPI.Models;

namespace ResumeAPI.Services;

// ──────────────────────────────────────────────
// Interface
// ──────────────────────────────────────────────

public interface IMatchService
{
    Task<MatchResponseDto> MatchAsync(MatchRequestDto dto);
}

// ──────────────────────────────────────────────
// Implementation
// ──────────────────────────────────────────────

/// <summary>
/// Orchestrates the job matching workflow:
///   1. Retrieve resume from DB
///   2. Call Python microservice for semantic similarity score
///   3. Run keyword analysis locally
///   4. Assemble and return the final match response
/// </summary>
public class MatchService : IMatchService
{
    private readonly IResumeService _resumeService;
    private readonly ISimilarityClient _similarityClient;
    private readonly ILogger<MatchService> _logger;

    public MatchService(
        IResumeService resumeService,
        ISimilarityClient similarityClient,
        ILogger<MatchService> logger)
    {
        _resumeService   = resumeService;
        _similarityClient = similarityClient;
        _logger          = logger;
    }

    /// <summary>
    /// Full pipeline: fetch resume → get similarity → extract keywords → return result.
    /// </summary>
    public async Task<MatchResponseDto> MatchAsync(MatchRequestDto dto)
    {
        // Step 1: Validate job description input
        if (string.IsNullOrWhiteSpace(dto.JobDescription))
            throw new ArgumentException("Job description cannot be empty.");

        // Step 2: Retrieve the resume from the data store
        var resume = await _resumeService.GetResumeByIdAsync(dto.ResumeId);
        if (resume == null)
            throw new KeyNotFoundException($"Resume with ID '{dto.ResumeId}' was not found.");

        _logger.LogInformation("Matching resume {Id} against provided job description.", dto.ResumeId);

        // Step 3: Call Python microservice to get semantic similarity score
        double score;
        try
        {
            score = await _similarityClient.GetSimilarityScoreAsync(resume.Content, dto.JobDescription);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to reach Python similarity service.");
            throw new InvalidOperationException(
                "Similarity service is unavailable. Please ensure the Python service is running on port 5001.", ex);
        }

        // Step 4: Run keyword analysis (purely local, no external calls)
        var (matched, missing) = KeywordExtractor.Compare(resume.Content, dto.JobDescription);

        // Step 5: Build and return the response
        return new MatchResponseDto
        {
            MatchScore       = score,
            MatchedKeywords  = matched,
            MissingKeywords  = missing,
            MatchLabel       = GetMatchLabel(score)
        };
    }

    /// <summary>
    /// Maps a numeric score to a human-readable label for quick interpretation.
    /// </summary>
    private static string GetMatchLabel(double score) => score switch
    {
        >= 80 => "Excellent Match",
        >= 60 => "Good Match",
        >= 40 => "Moderate Match",
        >= 20 => "Weak Match",
        _     => "Poor Match"
    };
}
