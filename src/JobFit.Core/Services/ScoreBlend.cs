namespace JobFit.Services;

/// <summary>
/// Final fit score (2.4): half "does the text read like this job" (semantic cosine x 100, or the keyword
/// proxy when Python is down) and half "does the resume have the posting's catalog skills".
///   coverage = matched catalog skills / all catalog skills the posting names (implied skills count)
///   score    = 0.5 x semantic + 0.5 x 100 x sqrt(coverage)
/// sqrt rewards the first skills more than the last few, so 18 of 26 (69%) is 83, not 69, and a near-perfect
/// resume can reach Excellent while 1 of 24 (4%) stays at 20. A posting with no catalog skills uses semantic only.
/// </summary>
public static class ScoreBlend
{
    public const double SemanticWeight = 0.5;

    public static double Coverage(int matched, int total) =>
        total <= 0 ? double.NaN : Math.Clamp((double)matched / total, 0, 1);

    public static double CoverageScore(int matched, int total)
    {
        var c = Coverage(matched, total);
        return double.IsNaN(c) ? double.NaN : 100 * Math.Sqrt(c);
    }

    public static double Blend(double semantic, int matched, int total)
    {
        var cov = CoverageScore(matched, total);
        semantic = Math.Clamp(semantic, 0, 100);
        return double.IsNaN(cov) ? semantic : SemanticWeight * semantic + (1 - SemanticWeight) * cov;
    }
}
