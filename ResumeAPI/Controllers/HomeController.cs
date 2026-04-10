using Microsoft.AspNetCore.Mvc;
using System.Text;
using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;
using ResumeAPI.DTOs;
using ResumeAPI.Services;
using ResumeAPI.ViewModels;

namespace ResumeAPI.Controllers;

/// <summary>
/// MVC controller for the interview/demo-friendly frontend.
/// </summary>
public class HomeController : Controller
{
    private readonly IResumeService _resumeService;
    private readonly IMatchService _matchService;
    private readonly ILogger<HomeController> _logger;

    public HomeController(
        IResumeService resumeService,
        IMatchService matchService,
        ILogger<HomeController> logger)
    {
        _resumeService = resumeService;
        _matchService = matchService;
        _logger = logger;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View(new ResumeMatchPageViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> MatchResume(ResumeMatchPageViewModel model)
    {
        var resumeId = model.ResumeId.GetValueOrDefault();
        var hasNewResumeFile = model.ResumeFile is not null && model.ResumeFile.Length > 0;

        if (resumeId == Guid.Empty && !hasNewResumeFile)
        {
            ModelState.AddModelError(nameof(model.ResumeFile), "Resume file is required.");
        }

        var hasTypedJobDescription = !string.IsNullOrWhiteSpace(model.JobDescriptionText);
        var hasNewJobDescriptionFile = model.JobDescriptionFile is not null && model.JobDescriptionFile.Length > 0;
        var hasExistingJobDescription = !string.IsNullOrWhiteSpace(model.JobDescriptionContent);

        if (!hasTypedJobDescription && !hasNewJobDescriptionFile && !hasExistingJobDescription)
        {
            ModelState.AddModelError(nameof(model.JobDescriptionText), "Add a job description by pasting text or uploading a file.");
        }

        if (!ModelState.IsValid)
        {
            model.PageError = "Please add a resume and a job description before running the match.";
            return View("Index", model);
        }

        try
        {
            if (resumeId == Guid.Empty || hasNewResumeFile)
            {
                var extractedResumeText = await ExtractPlainTextAsync(model.ResumeFile!);
                var resumeDisplayName = GetResumeDisplayName(model.ResumeFile!);
                var uploadResponse = await _resumeService.UploadResumeAsync(new ResumeUploadDto
                {
                    CandidateName = resumeDisplayName,
                    Content = extractedResumeText
                });

                model.ResumeId = uploadResponse.Id;
                model.ResumeDisplayName = resumeDisplayName;
                model.UploadedResumeFileName = model.ResumeFile!.FileName;
                model.UploadedResumePreview = BuildPreview(extractedResumeText);
                resumeId = uploadResponse.Id;
            }

            if (hasTypedJobDescription)
            {
                model.JobDescriptionContent = model.JobDescriptionText.Trim();
                model.UploadedJobDescriptionFileName = "Typed in page";
                model.UploadedJobDescriptionPreview = BuildPreview(model.JobDescriptionContent);
            }
            else if (hasNewJobDescriptionFile)
            {
                var extractedJobDescription = await ExtractPlainTextAsync(model.JobDescriptionFile!);
                model.JobDescriptionContent = extractedJobDescription;
                model.UploadedJobDescriptionFileName = model.JobDescriptionFile!.FileName;
                model.UploadedJobDescriptionPreview = BuildPreview(extractedJobDescription);
            }

            var response = await _matchService.MatchAsync(new MatchRequestDto
            {
                ResumeId = resumeId,
                JobDescription = model.JobDescriptionContent
            });

            model.MatchResult = new MatchResultViewModel
            {
                MatchScore = response.MatchScore,
                MatchLabel = response.MatchLabel,
                MatchedKeywords = response.MatchedKeywords,
                MissingKeywords = response.MissingKeywords,
                DetailedSummary = BuildDetailedSummary(
                    model.ResumeDisplayName,
                    response.MatchScore,
                    response.MatchedKeywords,
                    response.MissingKeywords),
                RecommendationSummary = BuildRecommendationSummary(
                    response.MatchScore,
                    response.MissingKeywords),
                InterviewQuestions = BuildInterviewQuestions(
                    model.JobDescriptionContent,
                    response.MatchedKeywords,
                    response.MissingKeywords)
            };

            model.PageMessage = "Match generated successfully.";
            model.PageError = null;
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Match request validation failed.");
            model.PageError = ex.Message;
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Resume not found during matching.");
            model.PageError = ex.Message;
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Similarity service unavailable.");
            model.PageError = ex.Message;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while matching resume.");
            model.PageError = "Unexpected error while matching resume. Please try again.";
        }

        return View("Index", model);
    }

    /// <summary>
    /// Reads text from uploaded files. This demo supports plain text formats only.
    /// </summary>
    private static async Task<string> ExtractPlainTextAsync(IFormFile file)
    {
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        var allowedExtensions = new[] { ".txt", ".md", ".csv", ".log", ".json", ".pdf", ".docx" };

        if (!allowedExtensions.Contains(extension))
            throw new ArgumentException("Unsupported file type. Upload .txt, .md, .csv, .log, .json, .pdf, or .docx.");

        string content = extension switch
        {
            ".pdf" => await ExtractPdfTextAsync(file),
            ".docx" => await ExtractDocxTextAsync(file),
            _ => await ExtractTextFileAsync(file)
        };

        if (string.IsNullOrWhiteSpace(content))
            throw new ArgumentException($"Uploaded file '{file.FileName}' is empty.");

        return content.Trim();
    }

    private static string BuildPreview(string text)
    {
        const int maxLength = 700;
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Length <= maxLength
            ? text
            : text[..maxLength] + "...";
    }

    private static string GetResumeDisplayName(IFormFile file)
    {
        var fileName = Path.GetFileNameWithoutExtension(file.FileName)?.Trim();
        return string.IsNullOrWhiteSpace(fileName) ? "Uploaded Resume" : fileName;
    }

    private static string BuildDetailedSummary(
        string resumeDisplayName,
        double score,
        IReadOnlyList<string> matchedKeywords,
        IReadOnlyList<string> missingKeywords)
    {
        var profileName = string.IsNullOrWhiteSpace(resumeDisplayName) ? "The uploaded resume" : resumeDisplayName;
        var topStrengths = matchedKeywords.Take(4).ToList();
        var topGaps = missingKeywords.Take(3).ToList();

        var opening = score switch
        {
            >= 85 => $"{profileName} appears highly aligned with the uploaded job description and already reflects many of the core expectations for the role.",
            >= 70 => $"{profileName} shows a strong overall fit for the uploaded role, with meaningful overlap across the main requirements.",
            >= 55 => $"{profileName} shows moderate alignment with the uploaded role, but some requirements would benefit from deeper validation during screening.",
            _ => $"{profileName} currently shows limited alignment with the uploaded role and would likely need more tailoring before moving forward."
        };

        var strengthsSentence = topStrengths.Count > 0
            ? $"The most visible strengths in the current match are {FormatHumanList(topStrengths)}, which suggests the resume already speaks to several important requirements."
            : "The overlap between the resume and the role is currently narrow, so the strongest qualifications are not yet clearly signaled.";

        var gapsSentence = topGaps.Count > 0
            ? $"The main areas to explore further are {FormatHumanList(topGaps)}, since those expectations are less visible in the current resume."
            : "No major requirement gaps were surfaced from the uploaded job description, which is a strong signal for role fit.";

        return $"{opening} {strengthsSentence} {gapsSentence}";
    }

    private static string BuildRecommendationSummary(double score, IReadOnlyList<string> missingKeywords)
    {
        if (score >= 85)
            return "Recommendation: move into interview discussion and validate depth, ownership, and business impact through real project examples.";

        if (score >= 70)
            return "Recommendation: shortlist the profile and focus interview time on validating the less visible requirements and execution depth.";

        if (score >= 55)
            return "Recommendation: consider a screening round to confirm whether the candidate's underlying experience is stronger than the resume currently communicates.";

        return missingKeywords.Count > 0
            ? $"Recommendation: before progressing, strengthen or clarify evidence around {FormatHumanList(missingKeywords.Take(3).ToList())}."
            : "Recommendation: the resume would benefit from clearer tailoring before progressing further.";
    }

    private static List<string> BuildInterviewQuestions(
        string jobDescriptionText,
        IReadOnlyList<string> matchedKeywords,
        IReadOnlyList<string> missingKeywords)
    {
        var questions = new List<string>();
        var strongTopics = matchedKeywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(4)
            .ToList();
        var gapTopics = missingKeywords
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(3)
            .ToList();
        var jdTopics = KeywordExtractor.ExtractKeywords(jobDescriptionText)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();

        foreach (var topic in strongTopics)
        {
            questions.Add($"Can you walk me through a project where you used {topic} and explain the outcome you achieved?");
        }

        foreach (var topic in gapTopics)
        {
            questions.Add($"This role values {topic}. What relevant experience do you have in that area, even if it is not emphasized in the resume?");
        }

        foreach (var topic in jdTopics)
        {
            if (questions.Count >= 10)
                break;

            questions.Add($"How would you approach delivering results in a role that places strong emphasis on {topic}?");
        }

        var fallbackQuestions = new[]
        {
            "Which project in your background is the best evidence that you can succeed in this role?",
            "What trade-offs have you handled when balancing delivery speed with quality in a project similar to this role?",
            "If you joined this team tomorrow, what would be your first technical or process priority and why?",
            "How do you usually ramp up on a requirement that is new or only lightly represented in your experience?",
            "What kind of collaboration or communication pattern helps you perform best in a role like this one?"
        };

        foreach (var fallbackQuestion in fallbackQuestions)
        {
            if (questions.Count >= 10)
                break;

            questions.Add(fallbackQuestion);
        }

        return questions
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
    }

    private static string FormatHumanList(IReadOnlyList<string> items)
    {
        if (items.Count == 0)
            return string.Empty;
        if (items.Count == 1)
            return items[0];
        if (items.Count == 2)
            return $"{items[0]} and {items[1]}";

        return $"{string.Join(", ", items.Take(items.Count - 1))}, and {items[^1]}";
    }

    private static async Task<string> ExtractTextFileAsync(IFormFile file)
    {
        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        return await reader.ReadToEndAsync();
    }

    private static async Task<string> ExtractPdfTextAsync(IFormFile file)
    {
        await using var source = file.OpenReadStream();
        using var memory = new MemoryStream();
        await source.CopyToAsync(memory);
        memory.Position = 0;

        var builder = new StringBuilder();
        using var pdf = PdfDocument.Open(memory);
        foreach (var page in pdf.GetPages())
        {
            builder.AppendLine(page.Text);
        }

        return builder.ToString();
    }

    private static async Task<string> ExtractDocxTextAsync(IFormFile file)
    {
        await using var source = file.OpenReadStream();
        using var memory = new MemoryStream();
        await source.CopyToAsync(memory);
        memory.Position = 0;

        using var document = WordprocessingDocument.Open(memory, false);
        var bodyText = document.MainDocumentPart?.Document.Body?.InnerText;
        return bodyText ?? string.Empty;
    }
}
