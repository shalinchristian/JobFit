using JobFit.Models;

namespace JobFit.Services;

/// <summary>
/// Turns missing skills into a short, deterministic action plan. Rule based, no external calls:
///   - a known skill with a curated related skill on the resume (SkillCatalog.AreRelated) -> point to that project (ShowProject)
///   - a known skill with nothing related                       -> learn it with a concrete tip (Learn)
///   - a curated soft skill (communication, leadership, ...)        -> show it on the resume (Reword)
///   - anything that is not a catalog skill                          -> skipped
/// Input order is kept within each group, so pass missing skills already sorted by importance.
/// </summary>
public static class GapPlanBuilder
{
    public const int DefaultMaxItems = 8;

    private static readonly HashSet<string> MobileKeys = new(StringComparer.OrdinalIgnoreCase)
    { "android", "kotlin", "ios", "swift", "jetpack compose", "jetpack", "flutter", "react native", "xml views" };

    private static readonly HashSet<string> WebKeys = new(StringComparer.OrdinalIgnoreCase)
    { "react", "angular", "vue", "next.js", "svelte", "html", "css", "javascript", "typescript" };

    /// <summary>Tips for mobile postings, replacing web-only tips (Lighthouse is a browser tool).</summary>
    private static readonly Dictionary<string, string> MobileTips = new(StringComparer.OrdinalIgnoreCase)
    {
        ["accessibility"] = "audit one screen with Android Accessibility Scanner (or Xcode's Accessibility Inspector) and TalkBack/VoiceOver, then fix the issues",
        ["web performance"] = "profile app startup and scrolling jank with Android Studio Profiler or Macrobenchmark",
        ["testing"] = "add unit tests for a ViewModel and one UI test for a screen",
    };

    /// <summary>The posting/resume is about mobile apps (and not also a web frontend role).</summary>
    public static bool IsMobileContext(IEnumerable<string> keys)
    {
        var list = keys.Select(k => SkillCatalog.Find(k)?.Key ?? k).ToList();
        return list.Count(MobileKeys.Contains) >= 2 && list.Count(WebKeys.Contains) < 2;
    }

    public static List<GapPlanItem> Build(
        IEnumerable<string> missingKeywords,
        IEnumerable<string> matchedKeywords,
        int maxItems = DefaultMaxItems)
    {
        var matched = matchedKeywords
            .Select(SkillCatalog.Find)
            .Where(s => s is not null)
            .Select(s => s!)
            .ToList();

        var missing = SkillCatalog.RankSkillsFirst(
            missingKeywords
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .Select(k => k.Trim().ToLowerInvariant())
                .Distinct());

        var mobile = IsMobileContext(matched.Select(m => m.Key).Concat(missing));
        var plan = new List<GapPlanItem>();
        var seenDisplays = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var keyword in missing)
        {
            if (plan.Count >= maxItems)
                break;

            var skill = SkillCatalog.Find(keyword);
            var display = skill?.Display ?? keyword;
            if (!seenDisplays.Add(display))
                continue; // e.g. "postgres" and "postgresql"

            if (skill is { Category: "soft" })
            {
                // Soft skills are rarely "learned" for one application; show them on the resume instead.
                var tip = char.ToUpperInvariant(skill.LearnTip[0]) + skill.LearnTip[1..];
                plan.Add(new GapPlanItem(display, GapAction.Reword, $"{tip}."));
            }
            else if (skill is not null)
            {
                var related = matched.FirstOrDefault(m => SkillCatalog.AreRelated(m.Key, skill.Key));
                plan.Add(related is not null
                    ? new GapPlanItem(display, GapAction.ShowProject,
                        $"You already list {related.Display}. Point to the project where you used it and add a line on how that maps to {display}.")
                    : new GapPlanItem(display, GapAction.Learn,
                        $"Learn {display}: {(mobile && MobileTips.TryGetValue(skill.Key, out var t) ? t : skill.LearnTip)}, then add it to your projects section."));
            }
            else
            {
                // Not a catalog skill: never becomes a gap-plan line (it used to produce "Reword <random word>").
                continue;
            }
        }

        return plan;
    }
}
