namespace JobFit.Models;

/// <summary>
/// Represents a resume stored in the system.
/// </summary>
public class Resume
{
    /// <summary>Unique identifier for the resume.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Candidate's full name.</summary>
    public string CandidateName { get; set; } = string.Empty;

    /// <summary>Raw text content of the resume.</summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>Timestamp when the resume was uploaded.</summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
