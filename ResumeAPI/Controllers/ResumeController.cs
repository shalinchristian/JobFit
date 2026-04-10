using Microsoft.AspNetCore.Mvc;
using ResumeAPI.DTOs;
using ResumeAPI.Services;

namespace ResumeAPI.Controllers;

/// <summary>
/// Handles resume upload operations.
/// Route: /api/resume
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class ResumeController : ControllerBase
{
    private readonly IResumeService _resumeService;
    private readonly ILogger<ResumeController> _logger;

    public ResumeController(IResumeService resumeService, ILogger<ResumeController> logger)
    {
        _resumeService = resumeService;
        _logger        = logger;
    }

    /// <summary>
    /// Uploads a resume (as plain text) and stores it for future matching.
    /// </summary>
    /// <param name="dto">Candidate name and resume text content.</param>
    /// <returns>The saved resume ID and metadata.</returns>
    /// <response code="201">Resume uploaded successfully.</response>
    /// <response code="400">Validation error — empty name or content.</response>
    [HttpPost("upload")]
    [ProducesResponseType(typeof(ResumeResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Upload([FromBody] ResumeUploadDto dto)
    {
        // Validate the incoming request
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var result = await _resumeService.UploadResumeAsync(dto);

            // Return 201 Created with the location header pointing to the new resource
            return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Validation failed during upload: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Retrieves a resume by its ID.
    /// Useful for verifying a successful upload.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var resume = await _resumeService.GetResumeByIdAsync(id);

        if (resume == null)
            return NotFound(new { error = $"Resume with ID '{id}' not found." });

        // Return a lightweight view — avoid exposing the full content in list/detail
        return Ok(new
        {
            resume.Id,
            resume.CandidateName,
            resume.UploadedAt,
            ContentPreview = resume.Content.Length > 200
                ? resume.Content[..200] + "..."
                : resume.Content
        });
    }
}
