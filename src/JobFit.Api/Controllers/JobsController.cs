using Microsoft.AspNetCore.Mvc;
using JobFit.DTOs;
using JobFit.Services;

namespace JobFit.Controllers;

/// <summary>
/// Job tracker API. Route: /api/jobs
/// </summary>
[ApiController]
[Route("api/jobs")]
[Produces("application/json")]
public class JobsController : ControllerBase
{
    private readonly IJobTrackerService _tracker;

    public JobsController(IJobTrackerService tracker) => _tracker = tracker;

    /// <summary>Lists tracked jobs, newest first.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<JobSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List() => Ok(await _tracker.ListAsync());

    /// <summary>Gets one job with its fit check, gap plan and interview questions.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(JobDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id)
    {
        var job = await _tracker.GetAsync(id);
        return job is null ? NotFound(new { error = $"Job '{id}' not found." }) : Ok(job);
    }

    /// <summary>Runs a fit check on resume text + job posting and saves it to the tracker.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(JobDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest request)
    {
        try
        {
            var job = await _tracker.CheckAndSaveAsync(request);
            return CreatedAtAction(nameof(Get), new { id = job.Id }, job);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Changes the status (Saved, Applied, Interview, Offer, Rejected).</summary>
    [HttpPatch("{id:guid}/status")]
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(JobDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        try
        {
            var job = await _tracker.UpdateStatusAsync(id, request.Status);
            return job is null ? NotFound(new { error = $"Job '{id}' not found." }) : Ok(job);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Deletes a tracked job.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id) =>
        await _tracker.DeleteAsync(id) ? NoContent() : NotFound(new { error = $"Job '{id}' not found." });
}
