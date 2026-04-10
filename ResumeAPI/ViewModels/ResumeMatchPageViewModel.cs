using Microsoft.AspNetCore.Http;

namespace ResumeAPI.ViewModels;

/// <summary>
/// View model that backs the single-page MVC UI for resume upload and matching.
/// </summary>
public class ResumeMatchPageViewModel
{
    public string ResumeDisplayName { get; set; } = string.Empty;

    public IFormFile? ResumeFile { get; set; }

    public IFormFile? JobDescriptionFile { get; set; }
    public string JobDescriptionText { get; set; } = string.Empty;

    // Stored after upload so matching can reference the same resume.
    public Guid? ResumeId { get; set; }
    public string UploadedResumeFileName { get; set; } = string.Empty;
    public string UploadedResumePreview { get; set; } = string.Empty;

    public string UploadedJobDescriptionFileName { get; set; } = string.Empty;
    public string UploadedJobDescriptionPreview { get; set; } = string.Empty;

    // Extracted plain text from uploaded JD file, kept for matching.
    public string JobDescriptionContent { get; set; } = string.Empty;

    public string? PageMessage { get; set; }
    public string? PageError { get; set; }

    public MatchResultViewModel? MatchResult { get; set; }

    public bool HasMatchResult => MatchResult is not null;
    public bool HasResumePreview => !string.IsNullOrWhiteSpace(UploadedResumePreview);
    public bool HasJobDescriptionPreview => !string.IsNullOrWhiteSpace(UploadedJobDescriptionPreview);
}

/// <summary>
/// UI-focused representation of the match response.
/// </summary>
public class MatchResultViewModel
{
    public double MatchScore { get; set; }
    public string MatchLabel { get; set; } = string.Empty;
    public List<string> MatchedKeywords { get; set; } = new();
    public List<string> MissingKeywords { get; set; } = new();
    public string DetailedSummary { get; set; } = string.Empty;
    public string RecommendationSummary { get; set; } = string.Empty;
    public List<string> InterviewQuestions { get; set; } = new();

    // Used by the Bootstrap progress bar.
    public int ScorePercentage => (int)Math.Clamp(Math.Round(MatchScore), 0, 100);

    public string ProgressBarCssClass => MatchScore switch
    {
        >= 80 => "bg-success",
        >= 60 => "bg-primary",
        >= 40 => "bg-warning",
        _ => "bg-danger"
    };

    public string ExecutiveSummary => MatchScore switch
    {
        >= 80 => "The resume appears strongly aligned with the target role and already reflects many of the core requirements.",
        >= 60 => "The resume shows a solid match overall, with good overlap across the main role expectations.",
        >= 40 => "The resume has partial alignment, but there are visible areas that may need clarification or strengthening.",
        _ => "The resume currently shows limited alignment with the role and would likely need significant tailoring."
    };

    public string StrengthsSummary => MatchedKeywords.Count switch
    {
        0 => "No strong keyword overlaps were detected from the uploaded documents.",
        1 => "One strong overlap was detected between the resume and the role requirements.",
        _ => $"{MatchedKeywords.Count} strong overlaps were detected between the resume and the role requirements."
    };

    public string GapsSummary => MissingKeywords.Count switch
    {
        0 => "No major keyword gaps were detected from the provided job description.",
        1 => "One important requirement may need to be addressed or explained during screening.",
        _ => $"{MissingKeywords.Count} requirement areas may need to be strengthened or explained during screening."
    };

    public bool HasInterviewQuestions => MatchScore >= 85 && InterviewQuestions.Count > 0;
}
