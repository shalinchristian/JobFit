using JobFit.DTOs;
using JobFit.Models;
using JobFit.Services;

namespace JobFit.ViewModels;

/// <summary>Backs the Check page form.</summary>
public class CheckFormViewModel
{
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public IFormFile? ResumeFile { get; set; }
    public string JobDescriptionText { get; set; } = string.Empty;
    public IFormFile? JobDescriptionFile { get; set; }
    public string? Error { get; set; }
}

/// <summary>Backs the Result page and the tracker detail page.</summary>
public class FitResultViewModel
{
    public Guid? SavedId { get; set; }
    public JobStatus? Status { get; set; }
    public DateTime? CheckedAt { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string ResumeName { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool UsedFallback { get; set; }
    public string JobDescription { get; set; } = string.Empty;
    public List<string> Matched { get; set; } = new();
    public List<string> Missing { get; set; } = new();
    public List<string> ResumeSkills { get; set; } = new();
    public List<GapPlanItem> GapPlan { get; set; } = new();
    public List<string> InterviewQuestions { get; set; } = new();
    public string Summary { get; set; } = string.Empty;

    public int ScoreRounded => ScoreBands.Round(Score);

    /// <summary>CSS tone for the score ring and label.</summary>
    public string Tone => ScoreBands.Tone(Score);

    public static FitResultViewModel FromDetail(JobDetailDto d, string jobDescription) => new()
    {
        SavedId = d.Id,
        Status = d.Status,
        CheckedAt = d.CreatedAt,
        JobTitle = d.Title,
        Company = d.Company,
        Score = d.Score,
        Label = d.Label,
        UsedFallback = d.UsedFallback,
        JobDescription = jobDescription,
        Matched = d.MatchedSkills,
        Missing = d.MissingSkills,
        GapPlan = d.GapPlan,
        InterviewQuestions = d.InterviewQuestions,
        Summary = MatchInsightsBuilder.BuildApplicantSummary(d.Score, d.MissingSkills.Where(SkillCatalog.IsSkill).Select(SkillCatalog.Display).ToList())
    };
}

public static class StatusUi
{
    public static readonly JobStatus[] All = Enum.GetValues<JobStatus>();
    public static string Css(JobStatus s) => s.ToString().ToLowerInvariant();

    /// <summary>"1 skill", "3 skills".</summary>
    public static string Plural(int n, string singular, string? plural = null) =>
        $"{n} {(n == 1 ? singular : plural ?? singular + "s")}";
}
