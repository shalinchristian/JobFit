using System.Text.RegularExpressions;

namespace JobFit.Services;

/// <summary>
/// Skill names that are also ordinary English (or single letters). They are NOT matched by the normal
/// lower-case phrase pass; instead each has a case-sensitive pattern and, where needed, a context rule
/// that must find a related word within <see cref="Window"/> characters.
///   Go     "Go"/"Golang" only; not "to go", "go live", "go-to", "Go through"
///   R      a standalone capital R; not "R&amp;D", not initials ("John R. Smith")
///   C      a standalone capital C near another language/programming word; not "C-level", "Series C", "C#", "C++"
///   Swift, Rust, Dart, Hilt, Dagger   capitalised only ("rust-proof", "swift delivery" don't count)
///   Express  "Express.js"/"ExpressJS" always; bare "Express" only near "Node"
///   Spring   "Spring Framework/MVC/Cloud/..." always; bare "Spring" only near Java/Kotlin/Hibernate
///   Excel    capitalised, and not "Excel at/in"
///   Flow     "Kotlin Flow"/"StateFlow"/"SharedFlow" always; bare "Flow" only near Kotlin/coroutines/Android
///   Room     capitalised "Room" near Android/Kotlin/database words
///   Ruby     not "Ruby on our team" (a name); "Ruby on Rails" is left to the phrase pass
/// Since 2.4: context is searched only inside the current sentence; a name glued to a hyphen ("Rust-free")
/// never counts; and a capitalised-only name that opens a sentence ("Swift turnaround...") needs tech
/// context in that sentence, because capitalisation there proves nothing.
/// </summary>
public static class AmbiguousSkills
{
    public const int Window = 80;

    /// <summary>Spellings the lower-case phrase pass must skip.</summary>
    public static readonly HashSet<string> CaseSensitivePhrases = new(StringComparer.OrdinalIgnoreCase)
    {
        "go", "golang", "go lang", "r", "c", "swift", "express", "spring", "rust", "dart",
        "room", "hilt", "dagger", "excel", "flow", "ruby",
    };

    private sealed record Rule(string Key, Regex Pattern, Regex? Near = null, Func<Match, bool>? NeedsContext = null, bool CaseOnly = false);

    private static Regex R(string p) => new(p, RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static Regex Ri(string p) => new(p, RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    private static readonly Regex AndroidContext = Ri(@"\b(android|kotlin|jetpack|compose|coroutines?|livedata|viewmodel|retrofit|hilt|sqlite|dao|local storage|persistence|database)\b");

    /// <summary>Tech context required when a capitalised-only name starts a sentence.</summary>
    private static readonly Regex TechContext = Ri(@"\b(ios|ipados|macos|xcode|swiftui|uikit|apple|mobile|android|kotlin|flutter|cargo|tokio|wasm|webassembly|programming|languages?|code|coding|developer|engineer|python|java|c\+\+|golang|typescript|javascript|compose|dagger|hilt|dependency injection|di|apps?|services?|apis?|crates?|frameworks?|libraries|library|backend|systems|memory)\b");

    private static readonly Regex SentenceStart = R(@"(?:[.!?:;]\s+|\n\s*|[\u2022\-*]\s+)$");

    private static readonly Regex SentenceBreak = R(@"[.!?](?=\s)|\n");

    private static readonly Rule[] Rules =
    {
        new("golang", R(@"(?<![\w\-./])(?:Golang|golang|GoLang|Go)(?![\w\-+'])(?!\s+(?:live|to|ahead|beyond|above|back|through|for|on|with|over|out|in|get|getter|far)\b)")),
        new("r", R(@"(?<![\w&./'\-])R(?![\w&+#'\-]|\s*&|\.\s*[A-Z]|\.[A-Za-z0-9])")),
        new("c", R(@"(?<![\w#+./\-])C(?![\w#+'\-]|\s*\+\+|\s*#|\s*&|\.\s*[A-Z]|\.[A-Za-z0-9])"),
            Ri(@"\b(c\+\+|python|java|rust|golang|embedded|firmware|programming|languages?|assembly|linux kernel|systems)\b")),
        new("swift", R(@"(?<![\w\-])Swift(?:UI)?(?![\w\-])"), CaseOnly: true),
        new("rust", R(@"(?<![\w\-])Rust(?![\w\-])"), CaseOnly: true),
        new("dart", R(@"(?<![\w\-])Dart(?![\w\-])"), CaseOnly: true),
        new("hilt", R(@"(?<![\w\-])Hilt(?![\w\-])"), CaseOnly: true),
        new("dagger", R(@"(?<![\w\-])Dagger(?:\s*2)?(?![\w\-])"), CaseOnly: true),
        new("express", R(@"(?<!\w)(?:Express(?:\.js|JS|js)?|express\.js|expressjs)(?![\w])"),
            Ri(@"\bnode"), m => !m.Value.Contains("js", StringComparison.OrdinalIgnoreCase)),
        new("spring", R(@"(?<!\w)Spring(?:\s+(?:Framework|MVC|Cloud|Security|Data|WebFlux))?(?!\w)(?![\s\-]+[Bb]oot)"),
            Ri(@"\b(java|kotlin|hibernate|jpa|maven)\b"), m => !m.Value.Contains(' ')),
        new("excel", R(@"(?<!\w)(?:Excel|EXCEL)(?!\w)(?!\s+(?:at|in)\b)")),
        new("flow", R(@"(?<!\w)(?:Kotlin\s+Flows?|StateFlow|SharedFlow|Flows?)(?!\w)"),
            Ri(@"\b(kotlin|coroutines?|livedata|android|viewmodel)\b"), m => m.Value is "Flow" or "Flows"),
        new("room", R(@"(?<!\w)Room(?!\w)"), AndroidContext, _ => true),
        new("ruby", R(@"(?<![\w\-])Ruby(?![\w\-])(?!\s+on\b)(?!\s+(?:is|was|said|and I|from|joined|will|has|leads?)\b)")),
    };

    /// <summary>All context-checked matches in the original (case-preserved) text.</summary>
    public static IEnumerable<(string Key, int Index, int Length)> Find(string text)
    {
        foreach (var rule in Rules)
        {
            foreach (Match m in rule.Pattern.Matches(text))
            {
                var around = new Lazy<string>(() => Around(text, m));
                if (rule.Near is not null && (rule.NeedsContext?.Invoke(m) ?? true) && !rule.Near.IsMatch(around.Value))
                    continue;
                if (rule.CaseOnly && StartsSentence(text, m.Index) && !TechContext.IsMatch(around.Value))
                    continue;
                yield return (rule.Key, m.Index, m.Length);
            }
        }
    }

    private static bool StartsSentence(string text, int index) =>
        SentenceStart.IsMatch(text[Math.Max(0, index - 4)..index]) || index == 0;

    /// <summary>Up to <see cref="Window"/> characters each side of the match, clipped to its sentence.</summary>
    private static string Around(string text, Match m)
    {
        var start = Math.Max(0, m.Index - Window);
        var end = Math.Min(text.Length, m.Index + m.Length + Window);
        foreach (Match b in SentenceBreak.Matches(text, start))
        {
            if (b.Index >= end) break;
            if (b.Index < m.Index) start = b.Index + 1;
            else if (b.Index >= m.Index + m.Length) { end = b.Index; break; }
        }
        return text[start..m.Index] + " " + text[(m.Index + m.Length)..end];
    }
}
