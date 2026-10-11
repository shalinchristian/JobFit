using Microsoft.AspNetCore.Mvc;
using JobFit.DTOs;
using JobFit.Services;

namespace JobFit.Controllers;

/// <summary>
/// Handles resume-to-job matching operations.
/// Route: /api/match
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MatchController : ControllerBase
{
    private readonly IMatchService _matchService;
    private readonly ILogger<MatchController> _logger;

    public MatchController(IMatchService matchService, ILogger<MatchController> logger)
    {
        _matchService = matchService;
        _logger       = logger;
    }

    /// <summary>
    /// Matches a stored resume against a provided job description.
    /// Returns a semantic similarity score plus keyword analysis.
    /// </summary>
    /// <param name="dto">Resume ID and job description text.</param>
    /// <returns>Match score, matched keywords, and missing keywords.</returns>
    /// <response code="200">Match computed successfully.</response>
    /// <response code="400">Empty job description or invalid input.</response>
    /// <response code="404">Resume ID not found.</response>
    /// <response code="503">Not returned in normal use: the keyword fallback is used when Python is down.</response>
    [HttpPost]
    [ProducesResponseType(typeof(MatchResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(object), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Match([FromBody] MatchRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _matchService.MatchAsync(dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Bad request for match: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Resume not found: {Message}", ex.Message);
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            // Python service is down
            _logger.LogError("Similarity service error: {Message}", ex.Message);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { error = ex.Message });
        }
    }

    /// <summary>
    /// Fit check on raw text, nothing stored: score, skills, gap plan and interview questions.
    /// Falls back to keyword scoring (usedFallback = true) if the Python service is down.
    /// </summary>
    [HttpPost("text")]
    [ProducesResponseType(typeof(FitCheckResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> MatchText([FromBody] FitCheckRequest dto)
    {
        try
        {
            var r = await _matchService.MatchTextAsync(dto.ResumeText, dto.JobDescription, new[] { dto.Company ?? "", dto.Title ?? "" });
            return Ok(new FitCheckResponse
            {
                MatchScore = r.MatchScore,
                SemanticScore = r.SemanticScore,
                SkillCoverage = r.SkillCoverage,
                MatchLabel = r.MatchLabel,
                UsedFallback = r.UsedFallback,
                MatchedKeywords = r.MatchedKeywords,
                MissingKeywords = r.MissingKeywords,
                GapPlan = GapPlanBuilder.Build(r.MissingKeywords, r.MatchedKeywords.Concat(r.ResumeSkills)),
                InterviewQuestions = MatchInsightsBuilder.BuildInterviewQuestions(
                    dto.JobDescription,
                    r.MatchedKeywords.Select(SkillCatalog.Display).ToList(),
                    r.MissingKeywords.Select(SkillCatalog.Display).ToList())
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}

public class FitCheckRequest
{
    public string ResumeText { get; set; } = string.Empty;
    public string JobDescription { get; set; } = string.Empty;
    /// <summary>Optional: words from the company name are never reported as skills.</summary>
    public string? Company { get; set; }
    /// <summary>Optional: non-skill words from the job title are never reported as skills.</summary>
    public string? Title { get; set; }
}

public class FitCheckResponse : MatchResponseDto
{
    public List<JobFit.Models.GapPlanItem> GapPlan { get; set; } = new();
    public List<string> InterviewQuestions { get; set; } = new();
}
