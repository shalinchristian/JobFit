namespace ResumeAPI.Services;

/// <summary>
/// Simple keyword extraction using token overlap.
/// Filters out common stop words and punctuation.
/// No NLP library needed — works well enough for demo purposes.
/// </summary>
public static class KeywordExtractor
{
    // Common English stop words to ignore
    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for",
        "of", "with", "by", "from", "is", "are", "was", "were", "be", "been",
        "have", "has", "had", "do", "does", "did", "will", "would", "could",
        "should", "may", "might", "must", "shall", "i", "we", "you", "he",
        "she", "it", "they", "this", "that", "these", "those", "as", "not",
        "our", "your", "their", "its", "my", "his", "her", "than", "then",
        "so", "if", "about", "up", "out", "into", "over", "also", "can"
    };

    /// <summary>
    /// Tokenizes the input text and returns a set of meaningful words.
    /// Strips punctuation, converts to lowercase, removes stop words.
    /// </summary>
    public static HashSet<string> ExtractKeywords(string text)
    {
        return text
            .Split(new[] { ' ', '\n', '\r', '\t', ',', '.', ';', ':', '(', ')', '-', '/', '\\' },
                   StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.Trim().ToLowerInvariant())
            .Where(w => w.Length > 2 && !StopWords.Contains(w))
            .ToHashSet();
    }

    /// <summary>
    /// Compares resume keywords against job description keywords.
    /// Returns matched keywords (present in both) and missing keywords (in JD but not resume).
    /// </summary>
    public static (List<string> Matched, List<string> Missing) Compare(string resumeText, string jobText)
    {
        var resumeKeywords = ExtractKeywords(resumeText);
        var jobKeywords    = ExtractKeywords(jobText);

        // Intersection: keywords the resume already covers
        var matched = jobKeywords.Intersect(resumeKeywords, StringComparer.OrdinalIgnoreCase)
                                 .OrderBy(k => k)
                                 .ToList();

        // Difference: job keywords absent from the resume
        var missing = jobKeywords.Except(resumeKeywords, StringComparer.OrdinalIgnoreCase)
                                 .OrderBy(k => k)
                                 .ToList();

        return (matched, missing);
    }
}
