using JobFit.Models;

namespace JobFit.DTOs;

/// <summary>Request to run a fit check and save it to the tracker in one call.</summary>
public class CreateJobRequest
{
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    /// <summary>Plain text of the resume.</summary>
    public string ResumeText { get; set; } = string.Empty;
    /// <summary>Plain text of the job posting.</summary>
    public string JobDescription { get; set; } = string.Empty;
    public JobStatus Status { get; set; } = JobStatus.Saved;
}

/// <summary>Request to save an already computed result.</summary>
public class SaveJobRequest
{
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool UsedFallback { get; set; }
    public string JobDescription { get; set; } = string.Empty;
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
    public List<string> ResumeSkills { get; set; } = new();
    public JobStatus Status { get; set; } = JobStatus.Saved;
}

public class UpdateStatusRequest
{
    public JobStatus Status { get; set; }
}

/// <summary>Row in the tracker list.</summary>
public class JobSummaryDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Label { get; set; } = string.Empty;
    public JobStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

/// <summary>Full detail: the fit check, gap plan and interview prep for one job.</summary>
public class JobDetailDto : JobSummaryDto
{
    public bool UsedFallback { get; set; }
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
    public List<GapPlanItem> GapPlan { get; set; } = new();
    public List<string> InterviewQuestions { get; set; } = new();
}
