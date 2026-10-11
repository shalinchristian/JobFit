namespace JobFit.Services;

/// <summary>
/// Keyword stand-in for the semantic half of the score when the Python service is unavailable. Since 2.4
/// MatchService blends it 50/50 with skill coverage exactly like the model score, so this only has to
/// track the raw MiniLM cosine. Floor/Span were refit (least squares) against the model on QA's 11
/// resume/posting pairs: mean gap 5.3 points before blending, about half that after.
///
///   coverage = 0.8 x (share of the posting's catalog skills on the resume, weighted by how often the posting
///              mentions each) + 0.2 x (share of the posting's other words on the resume)
///   score    = 15 + 70 x sqrt(coverage)          -> always between 20 and 85
///
/// A keyword match is never "certain", so it can't reach 100. Deterministic. Empty posting -> 0.
/// </summary>
public static class FallbackScorer
{
    public const double Floor = 20;
    public const double Span = 65;

    public static double Score(string resumeText, string jobText)
    {
        var jobTerms = KeywordExtractor.ExtractTerms(jobText ?? string.Empty);
        if (jobTerms.Count == 0)
            return 0;

        var resumeKeys = SkillCatalog.WithImplied(KeywordExtractor.ExtractTerms(resumeText ?? string.Empty).Select(t => t.Key));

        static double Share(IReadOnlyCollection<KeywordExtractor.Term> terms, HashSet<string> have, bool weighted)
        {
            if (terms.Count == 0) return double.NaN;
            double total = 0, covered = 0;
            foreach (var t in terms)
            {
                var w = weighted ? t.Count : 1;
                total += w;
                if (have.Contains(t.Key)) covered += w;
            }
            return covered / total;
        }

        var skills = Share(jobTerms.Where(t => t.IsSkill).ToList(), resumeKeys, weighted: true);
        var words = Share(jobTerms.Where(t => !t.IsSkill).ToList(), resumeKeys, weighted: false);

        var coverage = double.IsNaN(skills) ? words
                     : double.IsNaN(words) ? skills
                     : 0.8 * skills + 0.2 * words;

        return Math.Round(Floor + Span * Math.Sqrt(coverage), 1);
    }
}
