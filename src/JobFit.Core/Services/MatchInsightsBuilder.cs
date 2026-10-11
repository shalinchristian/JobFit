namespace JobFit.Services;

/// <summary>
/// Turns a raw match result into human-readable insights:
/// previews, a narrative summary, a recommendation, and interview questions.
/// Pure functions with no I/O, so they are easy to unit test.
/// </summary>
public static class MatchInsightsBuilder
{
    public static string BuildPreview(string text)
    {
        const int maxLength = 700;
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        return text.Length <= maxLength
            ? text
            : text[..maxLength] + "...";
    }

    public static string BuildDetailedSummary(
        string resumeDisplayName,
        double score,
        IReadOnlyList<string> matchedKeywords,
        IReadOnlyList<string> missingKeywords)
    {
        var profileName = string.IsNullOrWhiteSpace(resumeDisplayName) ? "The uploaded resume" : resumeDisplayName;
        var topStrengths = matchedKeywords.Take(4).ToList();
        var topGaps = missingKeywords.Take(3).ToList();

        var opening = ScoreBands.For(score) switch
        {
            FitBand.Excellent => $"{profileName} appears highly aligned with the uploaded job description and already reflects many of the core expectations for the role.",
            FitBand.Good => $"{profileName} shows a good overall fit for the uploaded role, with meaningful overlap across the main requirements.",
            FitBand.Moderate => $"{profileName} shows moderate alignment with the uploaded role, but some requirements would benefit from deeper validation during screening.",
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

    public static string BuildRecommendationSummary(double score, IReadOnlyList<string> missingKeywords)
    {
        var band = ScoreBands.For(score);
        if (band == FitBand.Excellent)
            return "Recommendation: move into interview discussion and validate depth, ownership, and business impact through real project examples.";

        if (band == FitBand.Good)
            return "Recommendation: shortlist the profile and focus interview time on validating the less visible requirements and execution depth.";

        if (band == FitBand.Moderate)
            return "Recommendation: consider a screening round to confirm whether the candidate's underlying experience is stronger than the resume currently communicates.";

        return missingKeywords.Count > 0
            ? $"Recommendation: before progressing, strengthen or clarify evidence around {FormatHumanList(missingKeywords.Take(3).ToList())}."
            : "Recommendation: the resume would benefit from clearer tailoring before progressing further.";
    }

    public static List<string> BuildInterviewQuestions(
        string jobDescriptionText,
        IReadOnlyList<string> matchedKeywords,
        IReadOnlyList<string> missingKeywords)
    {
        // Only catalog skills become question topics: generic words ("platform", "clear") make nonsense questions.
        static List<string> Skills(IEnumerable<string> items, int take) => items
            .Where(SkillCatalog.IsSkill)
            .Select(SkillCatalog.Display)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(take)
            .ToList();

        static bool IsSoft(string s) => SkillCatalog.Find(s)?.Category == "soft";

        var questions = new List<string>();
        var allMatched = Skills(matchedKeywords, int.MaxValue);
        var strongTopics = allMatched.Where(t => !IsSoft(t)).Take(4).ToList();
        var softTopics = Skills(matchedKeywords.Concat(missingKeywords), int.MaxValue).Where(IsSoft).Take(2).ToList();
        var gapTopics = Skills(missingKeywords, int.MaxValue).Where(t => !IsSoft(t)).Take(3).ToList();

        // "The posting also mentions X" only for skills that are neither matched nor already asked about.
        var skip = new HashSet<string>(allMatched.Concat(gapTopics).Concat(softTopics), StringComparer.OrdinalIgnoreCase);
        var jdTopics = Skills(KeywordExtractor.ExtractKeywords(jobDescriptionText ?? string.Empty), int.MaxValue)
            .Where(t => !skip.Contains(t) && !IsSoft(t))
            .Take(3)
            .ToList();

        foreach (var topic in strongTopics)
        {
            questions.Add($"Can you walk me through a project where you used {topic} and explain the outcome you achieved?");
        }

        foreach (var topic in gapTopics)
        {
            questions.Add($"This role values {topic}. What relevant experience do you have in that area, even if it is not emphasized in the resume?");
        }

        // Soft skills get behavioural phrasing, never "a project where you used Communication".
        foreach (var topic in softTopics)
        {
            questions.Add($"Tell me about a situation where {topic.ToLowerInvariant()} really mattered. What did you do, and what was the result?");
        }

        foreach (var topic in jdTopics)
        {
            if (questions.Count >= 10)
                break;

            questions.Add($"The posting also mentions {topic}. How have you used it, or how would you get productive with it in your first month?");
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

    /// <summary>
    /// Short, job-seeker facing verdict (the recruiter-style summaries above are kept for the API).
    /// </summary>
    public static string BuildApplicantSummary(double score, IReadOnlyList<string> missingSkills)
    {
        var gaps = missingSkills.Where(s => !string.IsNullOrWhiteSpace(s)).Take(3).ToList();
        var gapText = gaps.Count > 0 ? $" Biggest gaps: {FormatHumanList(gaps)}." : string.Empty;

        return ScoreBands.For(score) switch
        {
            FitBand.Excellent => $"Strong fit. Apply, and use the questions below to prepare.{gapText}",
            FitBand.Good => $"Good base. Close the top gaps in the plan below, then apply.{gapText}",
            FitBand.Moderate => $"Partial fit. Tailor your resume with the gap plan before you apply.{gapText}",
            _ => $"Long shot for now. Work through the gap plan or look for a closer role.{gapText}"
        };
    }

    public static string FormatHumanList(IReadOnlyList<string> items)
    {
        if (items.Count == 0)
            return string.Empty;
        if (items.Count == 1)
            return items[0];
        if (items.Count == 2)
            return $"{items[0]} and {items[1]}";

        return $"{string.Join(", ", items.Take(items.Count - 1))}, and {items[^1]}";
    }
}
