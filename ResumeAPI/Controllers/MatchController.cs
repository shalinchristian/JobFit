using Microsoft.AspNetCore.Mvc;
using ResumeAPI.DTOs;
using ResumeAPI.Services;

namespace ResumeAPI.Controllers;

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
    /// <response code="503">Python similarity service is unavailable.</response>
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
}
