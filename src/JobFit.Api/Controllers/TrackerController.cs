using Microsoft.AspNetCore.Mvc;
using JobFit.Models;
using JobFit.Services;
using JobFit.ViewModels;

namespace JobFit.Controllers;

/// <summary>Tracker pages: list, detail, change status, delete.</summary>
public class TrackerController : Controller
{
    private readonly IJobTrackerService _tracker;
    private readonly JobFit.Repositories.IJobRepository _repo;

    public TrackerController(IJobTrackerService tracker, JobFit.Repositories.IJobRepository repo)
    {
        _tracker = tracker;
        _repo = repo;
    }

    [HttpGet]
    public async Task<IActionResult> Index() => View(await _tracker.ListAsync());

    [HttpGet]
    public async Task<IActionResult> Details(Guid id)
    {
        var job = await _tracker.GetAsync(id);
        if (job is null)
            return NotFound();

        var entity = await _repo.GetByIdAsync(id);
        return View(FitResultViewModel.FromDetail(job, entity?.JobDescription ?? string.Empty));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Status(Guid id, JobStatus status, string? returnTo)
    {
        var job = await _tracker.UpdateStatusAsync(id, status);
        if (job is null)
            return NotFound();

        TempData["Toast"] = $"\"{job.Title}\" moved to {status}.";
        return returnTo == "details" ? RedirectToAction(nameof(Details), new { id }) : RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _tracker.DeleteAsync(id);
        TempData["Toast"] = "Job removed from your tracker.";
        return RedirectToAction(nameof(Index));
    }
}
