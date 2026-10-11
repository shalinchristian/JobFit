using JobFit.DTOs;
using JobFit.Models;
using JobFit.Repositories;

namespace JobFit.Services;

public interface IJobTrackerService
{
    Task<JobDetailDto> CheckAndSaveAsync(CreateJobRequest request);
    Task<JobDetailDto> SaveAsync(SaveJobRequest request);
    Task<List<JobSummaryDto>> ListAsync();
    Task<JobDetailDto?> GetAsync(Guid id);
    Task<JobDetailDto?> UpdateStatusAsync(Guid id, JobStatus status);
    Task<bool> DeleteAsync(Guid id);
}

/// <summary>Business logic for the job tracker: save fit checks, list, change status, delete.</summary>
public class JobTrackerService : IJobTrackerService
{
    private readonly IJobRepository _repo;
    private readonly IMatchService _matchService;
    private readonly ILogger<JobTrackerService> _logger;

    public JobTrackerService(IJobRepository repo, IMatchService matchService, ILogger<JobTrackerService> logger)
    {
        _repo = repo;
        _matchService = matchService;
        _logger = logger;
    }

    public async Task<JobDetailDto> CheckAndSaveAsync(CreateJobRequest request)
    {
        var match = await _matchService.MatchTextAsync(
            request.ResumeText, request.JobDescription, new[] { request.Company ?? string.Empty, request.Title ?? string.Empty });
        return await SaveAsync(new SaveJobRequest
        {
            Title = request.Title ?? string.Empty,
            Company = request.Company ?? string.Empty,
            Score = match.MatchScore,
            Label = match.MatchLabel,
            UsedFallback = match.UsedFallback,
            JobDescription = request.JobDescription,
            MatchedSkills = match.MatchedKeywords,
            MissingSkills = match.MissingKeywords,
            ResumeSkills = match.ResumeSkills,
            Status = request.Status
        });
    }

    public async Task<JobDetailDto> SaveAsync(SaveJobRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new ArgumentException("Job title is required.");
        if (!Enum.IsDefined(request.Status))
            throw new ArgumentException("Unknown status.");

        var now = DateTime.UtcNow;
        var job = new JobApplication
        {
            Title = request.Title.Trim(),
            Company = string.IsNullOrWhiteSpace(request.Company) ? "Unknown company" : request.Company.Trim(),
            Score = ScoreBands.Round(request.Score),
            Label = MatchService.GetMatchLabel(ScoreBands.Round(request.Score)), // always server-side; client label ignored
            UsedFallback = request.UsedFallback,
            JobDescription = request.JobDescription?.Trim() ?? string.Empty,
            MatchedSkills = Clean(request.MatchedSkills),
            MissingSkills = Clean(request.MissingSkills),
            ResumeSkills = Clean(request.ResumeSkills).Where(SkillCatalog.IsSkill).ToList(),
            Status = request.Status,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _repo.AddAsync(job);
        _logger.LogInformation("Saved job {Id} ({Title} at {Company})", job.Id, job.Title, job.Company);
        return ToDetail(job);
    }

    public async Task<List<JobSummaryDto>> ListAsync()
    {
        var jobs = await _repo.ListAsync();
        return jobs.OrderByDescending(j => j.CreatedAt).Select(ToSummary).ToList();
    }

    public async Task<JobDetailDto?> GetAsync(Guid id)
    {
        var job = await _repo.GetByIdAsync(id);
        return job is null ? null : ToDetail(job);
    }

    public async Task<JobDetailDto?> UpdateStatusAsync(Guid id, JobStatus status)
    {
        if (!Enum.IsDefined(status))
            throw new ArgumentException("Unknown status.");

        var job = await _repo.GetByIdAsync(id);
        if (job is null)
            return null;

        job.Status = status;
        job.UpdatedAt = DateTime.UtcNow;
        await _repo.UpdateAsync(job);
        return ToDetail(job);
    }

    public Task<bool> DeleteAsync(Guid id) => _repo.DeleteAsync(id);

    private static List<string> Clean(IEnumerable<string>? items) =>
        (items ?? Enumerable.Empty<string>())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(200)
            .ToList();

    private static JobSummaryDto ToSummary(JobApplication j) => new()
    {
        Id = j.Id, Title = j.Title, Company = j.Company, Score = ScoreBands.Round(j.Score), Label = ScoreBands.Label(j.Score), // recomputed on read: old rows keep no stale label
        Status = j.Status, CreatedAt = j.CreatedAt, UpdatedAt = j.UpdatedAt
    };

    public static JobDetailDto ToDetail(JobApplication j) => new()
    {
        Id = j.Id, Title = j.Title, Company = j.Company, Score = ScoreBands.Round(j.Score), Label = ScoreBands.Label(j.Score), // recomputed on read: old rows keep no stale label
        Status = j.Status, CreatedAt = j.CreatedAt, UpdatedAt = j.UpdatedAt,
        UsedFallback = j.UsedFallback,
        MatchedSkills = j.MatchedSkills,
        MissingSkills = j.MissingSkills,
        GapPlan = GapPlanBuilder.Build(j.MissingSkills, j.MatchedSkills.Concat(j.ResumeSkills)),
        InterviewQuestions = MatchInsightsBuilder.BuildInterviewQuestions(
            j.JobDescription,
            SkillCatalog.RankSkillsFirst(j.MatchedSkills).Select(SkillCatalog.Display).ToList(),
            SkillCatalog.RankSkillsFirst(j.MissingSkills).Select(SkillCatalog.Display).ToList())
    };
}
