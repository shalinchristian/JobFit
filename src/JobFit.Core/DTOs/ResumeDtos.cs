namespace JobFit.DTOs;

/// <summary>
/// DTO for uploading a new resume.
/// </summary>
public class ResumeUploadDto
{
    /// <summary>Candidate's full name.</summary>
    public string CandidateName { get; set; } = string.Empty;

    /// <summary>Plain text content of the resume.</summary>
    public string Content { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO returned after a resume is uploaded.
/// </summary>
public class ResumeResponseDto
{
    public Guid Id { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// DTO for requesting a match between a stored resume and a job description.
/// </summary>
public class MatchRequestDto
{
    /// <summary>ID of the previously uploaded resume.</summary>
    public Guid ResumeId { get; set; }

    /// <summary>Job description text to match against.</summary>
    public string JobDescription { get; set; } = string.Empty;
}

/// <summary>
/// Response DTO returned after computing a job match.
/// </summary>
public class MatchResponseDto
{
    /// <summary>Similarity score from 0 (no match) to 100 (perfect match).</summary>
    public double MatchScore { get; set; }

    /// <summary>Semantic part of the score (cosine x 100, or the keyword proxy in fallback mode).</summary>
    public double SemanticScore { get; set; }

    /// <summary>Percent of the posting's catalog skills found on the resume; null when the posting names none.</summary>
    public double? SkillCoverage { get; set; }

    /// <summary>Keywords found in both the resume and the job description.</summary>
    public List<string> MatchedKeywords { get; set; } = new();

    /// <summary>Keywords from the job description that are absent in the resume.</summary>
    public List<string> MissingKeywords { get; set; } = new();

    /// <summary>Human-readable match summary label.</summary>
    public string MatchLabel { get; set; } = string.Empty;

    /// <summary>True when the Python service was unreachable and the keyword-overlap fallback score was used.</summary>
    public bool UsedFallback { get; set; }

    /// <summary>All catalog skills found anywhere in the resume (used to find related skills for the gap plan).</summary>
    public List<string> ResumeSkills { get; set; } = new();
}

/// <summary>
/// Internal DTO sent to the Python similarity microservice.
/// </summary>
public class SimilarityRequestDto
{
    public string Text1 { get; set; } = string.Empty;
    public string Text2 { get; set; } = string.Empty;
}

/// <summary>
/// Internal DTO received from the Python similarity microservice.
/// </summary>
public class SimilarityResponseDto
{
    public double Score { get; set; }
}
