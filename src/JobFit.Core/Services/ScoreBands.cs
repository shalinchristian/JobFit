namespace JobFit.Services;

public enum FitBand { Poor, Weak, Moderate, Good, Excellent }

/// <summary>
/// The one set of score cut-offs used everywhere (labels, applicant summary, recruiter recommendation,
/// UI colours). Tuned to the real range of all-MiniLM-L6-v2 cosine scores on resume vs. posting text:
/// Since 2.4 the score is a 50/50 blend of semantic cosine and catalog-skill coverage (see <see cref="ScoreBlend"/>).
/// QA's strong / medium / weak backend resumes blend to about 89 / 58 / 24, and a near-complete Android
/// resume to about 73, so Excellent starts at 72 and Weak at 20. The quick (fallback) score uses the same blend.
/// </summary>
public static class ScoreBands
{
    public const double Excellent = 72;
    public const double Good = 55;
    public const double Moderate = 40;
    public const double Weak = 20;

    /// <summary>
    /// The single rounding step: every displayed score, label, band and colour uses this whole number,
    /// so "72" can never show as "Good" (71.6 rounds to 72 = Excellent; 54.5 rounds to 55 = Good).
    /// </summary>
    public static int Round(double score) =>
        (int)Math.Clamp(Math.Round(score, MidpointRounding.AwayFromZero), 0, 100);

    public static FitBand For(double score) => (double)Round(score) switch
    {
        >= Excellent => FitBand.Excellent,
        >= Good => FitBand.Good,
        >= Moderate => FitBand.Moderate,
        >= Weak => FitBand.Weak,
        _ => FitBand.Poor
    };

    public static string Label(double score) => $"{For(score)} Match";

    /// <summary>CSS tone name used by the UI.</summary>
    public static string Tone(double score) => For(score) switch
    {
        FitBand.Excellent => "great",
        FitBand.Good => "good",
        FitBand.Moderate => "fair",
        _ => "low"
    };
}
