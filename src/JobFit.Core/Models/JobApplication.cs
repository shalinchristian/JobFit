namespace JobFit.Models;

/// <summary>Where a tracked job application currently stands.</summary>
public enum JobStatus
{
    Saved = 0,
    Applied = 1,
    Interview = 2,
    Offer = 3,
    Rejected = 4
}

/// <summary>A job the user checked their resume against and saved to the tracker.</summary>
public class JobApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public double Score { get; set; }
    public string Label { get; set; } = string.Empty;
    public JobStatus Status { get; set; } = JobStatus.Saved;
    public bool UsedFallback { get; set; }
    public string JobDescription { get; set; } = string.Empty;
    public List<string> MatchedSkills { get; set; } = new();
    public List<string> MissingSkills { get; set; } = new();
    /// <summary>Catalog skills found in the resume (not the resume text itself).</summary>
    public List<string> ResumeSkills { get; set; } = new();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
