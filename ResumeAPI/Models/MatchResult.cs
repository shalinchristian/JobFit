namespace ResumeAPI.Models;

/// <summary>
/// Represents the result of matching a resume against a job description.
/// </summary>
public class MatchResult
{
    /// <summary>Similarity score between 0 and 100.</summary>
    public double Score { get; set; }

    /// <summary>Keywords found in both the resume and job description.</summary>
    public List<string> MatchedKeywords { get; set; } = new();

    /// <summary>Keywords in the job description that are missing from the resume.</summary>
    public List<string> MissingKeywords { get; set; } = new();
}
