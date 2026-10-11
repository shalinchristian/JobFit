using JobFit.DTOs;

namespace JobFit.Services;

public interface IMatchService
{
    /// <summary>Matches a stored resume (by id) against a job description.</summary>
    Task<MatchResponseDto> MatchAsync(MatchRequestDto dto);

    /// <summary>Matches raw resume text against a job description (nothing is stored).</summary>
    Task<MatchResponseDto> MatchTextAsync(string resumeText, string jobDescription);

    /// <summary>
    /// Same, but words in <paramref name="ignoreTerms"/> (company name, job title, city) are never
    /// reported as matched or missing.
    /// </summary>
    Task<MatchResponseDto> MatchTextAsync(string resumeText, string jobDescription, IEnumerable<string> ignoreTerms);
}

/// <summary>
/// Orchestrates the fit check:
///   1. semantic score from the Python service (falls back to a C# keyword-overlap score if it is down)
///   2. local keyword analysis (matched / missing, real skills ranked first)
///   3. blended score (50% semantic, 50% catalog-skill coverage, see ScoreBlend) and label
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
        _resumeService    = resumeService;
        _similarityClient = similarityClient;
        _logger           = logger;
    }

    public async Task<MatchResponseDto> MatchAsync(MatchRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.JobDescription))
            throw new ArgumentException("Job description cannot be empty.");

        var resume = await _resumeService.GetResumeByIdAsync(dto.ResumeId);
        if (resume == null)
            throw new KeyNotFoundException($"Resume with ID '{dto.ResumeId}' was not found.");

        _logger.LogInformation("Matching resume {Id} against provided job description.", dto.ResumeId);
        return await MatchTextAsync(resume.Content, dto.JobDescription);
    }

    public Task<MatchResponseDto> MatchTextAsync(string resumeText, string jobDescription) =>
        MatchTextAsync(resumeText, jobDescription, Array.Empty<string>());

    public async Task<MatchResponseDto> MatchTextAsync(string resumeText, string jobDescription, IEnumerable<string> ignoreTerms)
    {
        if (string.IsNullOrWhiteSpace(jobDescription))
            throw new ArgumentException("Job description cannot be empty.");
        if (string.IsNullOrWhiteSpace(resumeText))
            throw new ArgumentException("Resume content cannot be empty.");

        double score;
        var usedFallback = false;
        try
        {
            score = await _similarityClient.GetSimilarityScoreAsync(resumeText, jobDescription);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            _logger.LogWarning(ex, "Python similarity service unavailable; using keyword-overlap fallback score.");
            score = FallbackScorer.Score(resumeText, jobDescription);
            usedFallback = true;
        }

        var (matched, missing) = KeywordExtractor.Compare(resumeText, jobDescription, ignoreTerms);
        var semantic = Math.Round(Math.Clamp(score, 0, 100), 1);
        // 50% semantic + 50% catalog-skill coverage, then round once; label, band, colour, tracker use this integer.
        score = ScoreBands.Round(ScoreBlend.Blend(score, matched.Count, matched.Count + missing.Count));
        var coverage = ScoreBlend.Coverage(matched.Count, matched.Count + missing.Count);

        return new MatchResponseDto
        {
            MatchScore      = score,
            SemanticScore   = semantic,
            SkillCoverage   = double.IsNaN(coverage) ? null : Math.Round(coverage * 100, 1),
            MatchedKeywords = matched,
            MissingKeywords = missing,
            MatchLabel      = GetMatchLabel(score),
            UsedFallback    = usedFallback,
            ResumeSkills    = KeywordExtractor.ExtractTerms(resumeText).Where(t => t.IsSkill).Select(t => t.Key).ToList()
        };
    }

    /// <summary>Label from the shared <see cref="ScoreBands"/> cut-offs.</summary>
    public static string GetMatchLabel(double score) => ScoreBands.Label(score);
}
