using Microsoft.AspNetCore.Mvc;
using JobFit.DTOs;
using JobFit.Models;
using JobFit.Services;
using JobFit.ViewModels;

namespace JobFit.Controllers;

/// <summary>Check page (upload resume + posting) and Result page.</summary>
public class HomeController : Controller
{
    private readonly IMatchService _matchService;
    private readonly IJobTrackerService _tracker;
    private readonly ILogger<HomeController> _logger;

    public HomeController(IMatchService matchService, IJobTrackerService tracker, ILogger<HomeController> logger)
    {
        _matchService = matchService;
        _tracker = tracker;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index(string? error) => View(new CheckFormViewModel
    {
        Error = error == "too-large" ? DocumentTextExtractor.TooLargeMessage(null) : null
    });

    [HttpGet]
    public IActionResult Check() => RedirectToAction(nameof(Index));

    [HttpPost]
    [ValidateAntiForgeryToken]
    // The request limit is well above the per-file limit so oversized files reach the action and get a
    // friendly message instead of Kestrel resetting the connection mid-upload.
    [RequestSizeLimit(DocumentTextExtractor.MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = DocumentTextExtractor.MaxRequestBytes)]
    public async Task<IActionResult> Check(CheckFormViewModel form)
    {
        var hasResume = form.ResumeFile is { Length: > 0 };
        var hasTypedJob = !string.IsNullOrWhiteSpace(form.JobDescriptionText);
        var hasJobFile = form.JobDescriptionFile is { Length: > 0 };

        // A chosen-but-empty file gets its own message (browsers send a 0-byte part with the file name).
        var emptyFile = new[] { form.ResumeFile, hasTypedJob ? null : form.JobDescriptionFile }
            .FirstOrDefault(f => f is { Length: 0 } && !string.IsNullOrWhiteSpace(f.FileName));
        if (emptyFile is not null)
        {
            form.Error = DocumentTextExtractor.EmptyMessage(emptyFile.FileName);
            return View("Index", form);
        }

        if (string.IsNullOrWhiteSpace(form.JobTitle) || !hasResume || (!hasTypedJob && !hasJobFile))
        {
            form.Error = "Add a job title, your resume and the job posting to run a fit check.";
            return View("Index", form);
        }

        var tooBig = new[] { form.ResumeFile, form.JobDescriptionFile }
            .FirstOrDefault(f => f is not null && f.Length > DocumentTextExtractor.MaxFileBytes);
        if (tooBig is not null)
        {
            form.Error = DocumentTextExtractor.TooLargeMessage(tooBig.FileName);
            return View("Index", form);
        }

        try
        {
            var resumeText = await ExtractAsync(form.ResumeFile!);
            var jobText = hasTypedJob ? form.JobDescriptionText.Trim() : await ExtractAsync(form.JobDescriptionFile!);

            var match = await _matchService.MatchTextAsync(resumeText, jobText, new[] { form.Company ?? "", form.JobTitle });
            var display = (Func<string, string>)SkillCatalog.Display;

            var vm = new FitResultViewModel
            {
                JobTitle = form.JobTitle.Trim(),
                Company = form.Company?.Trim() ?? string.Empty,
                ResumeName = form.ResumeFile!.FileName,
                Score = match.MatchScore, // already rounded once in MatchService
                Label = match.MatchLabel,
                UsedFallback = match.UsedFallback,
                JobDescription = jobText,
                Matched = match.MatchedKeywords,
                Missing = match.MissingKeywords,
                GapPlan = GapPlanBuilder.Build(match.MissingKeywords, match.MatchedKeywords.Concat(match.ResumeSkills)),
                ResumeSkills = match.ResumeSkills,
                InterviewQuestions = MatchInsightsBuilder.BuildInterviewQuestions(
                    jobText,
                    match.MatchedKeywords.Select(display).ToList(),
                    match.MissingKeywords.Select(display).ToList()),
                Summary = MatchInsightsBuilder.BuildApplicantSummary(
                    match.MatchScore, match.MissingKeywords.Where(SkillCatalog.IsSkill).Select(display).ToList())
            };
            return View("Result", vm);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Fit check input rejected.");
            form.Error = ex.Message;
            return View("Index", form);
        }
    }

    /// <summary>Saves the result shown on the Result page to the tracker.</summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(SaveJobRequest request)
    {
        try
        {
            request.MatchedSkills = SplitLines(Request.Form["MatchedSkillsText"]);
            request.MissingSkills = SplitLines(Request.Form["MissingSkillsText"]);
            request.ResumeSkills = SplitLines(Request.Form["ResumeSkillsText"]);
            var saved = await _tracker.SaveAsync(request);
            TempData["Toast"] = $"Saved \"{saved.Title}\" to your tracker.";
            return RedirectToAction("Details", "Tracker", new { id = saved.Id });
        }
        catch (ArgumentException ex)
        {
            TempData["Toast"] = ex.Message;
            return RedirectToAction(nameof(Index));
        }
    }

    private static List<string> SplitLines(string? value) =>
        (value ?? string.Empty).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

    private static async Task<string> ExtractAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        return await DocumentTextExtractor.ExtractPlainTextAsync(stream, file.FileName);
    }
}
