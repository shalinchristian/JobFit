using System.Text.RegularExpressions;

namespace JobFit.Services;

/// <summary>
/// Keyword analysis without an NLP library:
///   1. catalog-aware phrase matching first ("ASP.NET Core", "Node.js", "CI/CD", "GitHub Actions" stay whole),
///   2. the remaining text is split into words, stop words / posting filler / places are dropped,
///   3. words are normalised with a tiny stemmer (apis -> api, mentored -> mentor) so plurals and tenses match.
/// Catalog skills are returned as their canonical key ("asp.net core"); other words as they first appear.
/// </summary>
public static class KeywordExtractor
{
    /// <summary>Hard cap on analysed text so a huge upload can't make the regex pass slow.</summary>
    public const int MaxAnalyzedChars = 200_000;


    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        // English
        "a", "an", "the", "and", "or", "but", "in", "on", "at", "to", "for", "of", "with", "by", "from",
        "is", "are", "was", "were", "be", "been", "being", "have", "has", "had", "do", "does", "did", "will",
        "would", "could", "should", "may", "might", "must", "shall", "i", "we", "you", "he", "she", "it",
        "they", "this", "that", "these", "those", "as", "not", "our", "your", "their", "its", "my", "his",
        "her", "than", "then", "so", "if", "about", "up", "out", "into", "over", "also", "can", "who", "what",
        "which", "where", "when", "how", "all", "any", "more", "most", "other", "such", "very", "just", "etc",
        "including", "within", "across", "via", "per", "every", "each", "both", "some", "many", "few", "own",
        "us", "them", "there", "here", "now", "well", "like", "get", "got", "make", "made", "new", "plus",
        "you'll", "we're", "we'll", "you're", "it's", "able", "ability", "around", "while", "through", "after",
        "before", "between", "during", "without", "under", "using", "use", "used", "based", "related",
        // Job-posting filler
        "apply", "applying", "application", "applications", "salary", "competitive", "compensation", "benefits",
        "perks", "offer", "offers", "bonus", "equity", "insurance", "leave", "holidays", "hybrid", "remote",
        "onsite", "on-site", "office", "flexible", "hours", "location", "relocation", "full-time", "part-time",
        "contract", "permanent", "senior", "junior", "lead", "principal", "staff", "intern", "mid", "level",
        "engineer", "engineers", "engineering", "developer", "developers", "development", "programmer",
        "backend", "back-end", "frontend", "front-end", "full-stack", "fullstack", "stack", "software",
        "design", "designs", "designing", "build", "building", "builds", "maintain", "maintaining", "write",
        "writing", "ship", "shipping", "deliver", "delivering", "own", "owning", "drive", "driving", "run",
        "running", "work", "working", "works", "join", "joining", "hiring", "hire", "seeking", "looking",
        "role", "roles", "position", "candidate", "candidates", "team", "teams", "teammates", "company",
        "product", "products", "platform", "platforms", "service", "services", "solution", "solutions",
        "system", "systems", "budget", "learning", "clear", "collaborate", "collaborating", "collaboration",
        "closely", "strong", "good", "great", "excellent", "solid", "proven", "nice", "preferred", "required",
        "requirement", "requirements", "responsibilities", "responsibility", "qualifications", "skills", "skill",
        "knowledge", "understanding", "familiarity", "experience", "experienced", "years", "year", "months",
        "plus", "bonus", "help", "helps", "want", "day", "days", "week", "weeks", "fast", "paced", "fast-paced",
        "environment", "opportunity", "opportunities", "passionate", "motivated", "about", "role:", "power",
        "powers", "performant", "high", "quality", "scalable", "best", "practices", "tools", "tool", "tech",
        "technology", "technologies", "stack", "modern", "world", "class", "users", "customers", "clients",
        "manager", "managers", "member", "members", "people", "ideal", "you've", "we've", "please", "now!",
        "offer:", "values", "value", "matters", "basic", "hands-on", "end", "least", "minimum", "degree",
        "bachelor", "bachelor's", "master", "equivalent", "field", "computer", "science", "etc.", "e.g",
        // Resume section headers / contact filler
        "summary", "profile", "objective", "education", "projects", "project", "present", "current",
        "university", "college", "school", "email", "phone", "linkedin", "github.com", "references", "india",
        // Months
        "january", "february", "march", "april", "june", "july", "august", "september", "october",
        "november", "december", "jan", "feb", "mar", "apr", "jun", "jul", "aug", "sep", "sept", "oct", "nov", "dec",
        // Common cities / countries (a location is never a skill gap)
        "pune", "mumbai", "bangalore", "bengaluru", "delhi", "noida", "gurgaon", "gurugram", "hyderabad",
        "chennai", "kolkata", "ahmedabad", "jaipur", "kochi", "indore", "chandigarh", "london", "berlin",
        "amsterdam", "paris", "dublin", "toronto", "vancouver", "singapore", "dubai", "sydney", "melbourne",
        "york", "san", "francisco", "seattle", "austin", "boston", "chicago", "usa", "uk", "germany",
        "canada", "australia", "europe", "emea", "apac", "worldwide", "anywhere",
    };

    private static readonly Regex WordSplit = new(@"[^a-z0-9#+'\-]+", RegexOptions.Compiled);

    private static readonly Regex PhraseRegex = BuildPhraseRegex();

    private static readonly Regex NotSkillPhrases = new(
        @"\b(senior|executive|company|business) leadership\b|\bleadership team\b|\bcommunication (channels|tools|platform)\b|\bfor networking\b|\bnetworking (events?|opportunit(y|ies))\b|\breact (quickly|fast|swiftly|calmly|well|to|when|in time)\b|\bspring[\s\-]+boot[\s\-]*camps?\b",
        RegexOptions.Compiled);

    private static Regex BuildPhraseRegex()
    {
        var alternatives = SkillCatalog.Phrases.Select(p => Regex.Escape(p.Phrase).Replace(@"\ ", @"[\s\-]+"));
        // A phrase must not be glued to letters/digits on either side ("reactive" is not "react").
        return new Regex(@"(?<![a-z0-9#+])(?:" + string.Join("|", alternatives) + @")(?![a-z0-9#+]|\.[a-z0-9])",
            RegexOptions.Compiled);
    }

    /// <summary>One distinct term found in a text.</summary>
    public record Term(string Key, string Display, bool IsSkill, int Count, int FirstIndex);

    /// <summary>Extracts terms with frequency and first position (used for importance ordering).</summary>
    public static List<Term> ExtractTerms(string text, IEnumerable<string>? ignore = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<Term>();

        var lower = (text.Length > MaxAnalyzedChars ? text[..MaxAnalyzedChars] : text).ToLowerInvariant();
        var ignored = new HashSet<string>((ignore ?? Enumerable.Empty<string>()).Select(Normalize), StringComparer.Ordinal);
        var terms = new Dictionary<string, (string Display, bool IsSkill, int Count, int First)>();

        void Add(string key, string display, bool isSkill, int index)
        {
            if (terms.TryGetValue(key, out var t))
                terms[key] = (t.Display, t.IsSkill, t.Count + 1, t.First);
            else
                terms[key] = (display, isSkill, 1, index);
        }

        // 0) phrases that look like skills but aren't ("present to senior leadership" is an audience, not a skill)
        lower = NotSkillPhrases.Replace(lower, m => new string(' ', m.Length));

        var chars = lower.ToCharArray();

        // 1a) ambiguous names (Go, R, C, Swift, Express, Excel, Room...) need case + context on the original text
        var original = text.Length > MaxAnalyzedChars ? text[..MaxAnalyzedChars] : text;
        if (original.Length == lower.Length)
        {
            foreach (var (key, index, length) in AmbiguousSkills.Find(original))
            {
                if (chars[index] == ' ') continue; // already consumed
                Add(key, key, true, index);
                for (var i = index; i < index + length; i++)
                    chars[i] = ' ';
            }
        }
        lower = new string(chars);

        // 1b) catalog phrases
        foreach (Match m in PhraseRegex.Matches(lower))
        {
            var phrase = Regex.Replace(m.Value, @"[\s\-]+", " ");
            var skill = SkillCatalog.Find(phrase) ?? SkillCatalog.Find(m.Value);
            if (skill is null)
                continue;
            Add(skill.Key, skill.Key, true, m.Index);
            if (skill.Key == "rails" && m.Value.StartsWith("ruby", StringComparison.Ordinal))
                Add("ruby", "ruby", true, m.Index); // "Ruby on Rails" names both the language and the framework
            for (var i = m.Index; i < m.Index + m.Length; i++)
                chars[i] = ' ';
        }

        // 2) remaining words
        var rest = new string(chars);
        var pos = 0;
        foreach (var raw in WordSplit.Split(rest))
        {
            var idx = raw.Length == 0 ? pos : rest.IndexOf(raw, pos, StringComparison.Ordinal);
            if (idx >= 0) pos = idx + raw.Length;

            var word = raw.Trim('\'', '-', '+');
            if (word.Length <= 2 || word.All(c => char.IsDigit(c) || c == '+' || c == '-'))
                continue;
            if (StopWords.Contains(word))
                continue;

            var stem = Normalize(word);
            if (stem.Length <= 2 || StopWords.Contains(stem) || ignored.Contains(stem))
                continue;
            if (AmbiguousSkills.CaseSensitivePhrases.Contains(stem) || AmbiguousSkills.CaseSensitivePhrases.Contains(word))
                continue; // "excel at", "room for", "flow": ordinary English unless the context pass claimed it

            Add(stem, word, false, idx < 0 ? int.MaxValue : idx);
        }

        return terms
            .Select(kv => new Term(kv.Key, kv.Value.Display, kv.Value.IsSkill, kv.Value.Count, kv.Value.First))
            .ToList();
    }

    /// <summary>Tiny, deterministic stemmer: plurals and common verb endings.</summary>
    public static string Normalize(string word)
    {
        var w = word.Trim().ToLowerInvariant();
        if (w.Length <= 3 || SkillCatalog.IsSkill(w))
            return w;

        if (w.EndsWith("ies") && w.Length > 4) w = w[..^3] + "y";
        else if (w.EndsWith("sses")) w = w[..^2];
        else if (w.EndsWith("ches") || w.EndsWith("shes") || w.EndsWith("xes") || w.EndsWith("zes")) w = w[..^2];
        else if (w.EndsWith("s") && !w.EndsWith("ss") && !w.EndsWith("us") && !(w.EndsWith("is") && w.Length > 5)) w = w[..^1];

        if (w.EndsWith("ied") && w.Length > 5) w = w[..^3] + "y";
        else if (w.EndsWith("ed") && w.Length > 5) w = w[..^2];
        else if (w.EndsWith("ing") && w.Length > 6) w = w[..^3];

        // Undo doubled consonants ("planned" -> "plann" -> "plan") and drop a silent e ("manage"/"managed" -> "manag").
        if (w.Length > 4 && w[^1] == w[^2] && !"aeiouls".Contains(w[^1])) w = w[..^1];
        if (w.Length > 4 && w.EndsWith('e')) w = w[..^1];
        return w;
    }

    /// <summary>Distinct keywords of a text, in order of first appearance.</summary>
    public static HashSet<string> ExtractKeywords(string text) =>
        ExtractTerms(text).OrderBy(t => t.FirstIndex).Select(t => t.Key).ToHashSet();

    /// <summary>
    /// Compares resume and posting. Only <see cref="SkillCatalog"/> skills are reported (non-catalog words were
    /// too noisy to show as "skills"; they still count toward the fallback score). Both lists are ordered by
    /// importance: how often the posting mentions the skill, then its first position.
    /// Words in <paramref name="ignore"/> (company, title, city) are never reported.
    /// </summary>
    public static (List<string> Matched, List<string> Missing) Compare(
        string resumeText, string jobText, IEnumerable<string>? ignore = null)
    {
        var ignoreWords = (ignore ?? Enumerable.Empty<string>())
            .SelectMany(s => WordSplit.Split((s ?? string.Empty).ToLowerInvariant()))
            .Where(w => w.Length > 1 && !SkillCatalog.IsSkill(w))
            .ToList();

        // Specific tools count for their umbrella skill (Jenkins -> CI/CD, EF Core -> .NET, EKS -> Kubernetes).
        var resumeKeys = SkillCatalog.WithImplied(ExtractTerms(resumeText).Select(t => t.Key));
        var jobTerms = ExtractTerms(jobText, ignoreWords)
            .OrderByDescending(t => t.IsSkill)
            .ThenByDescending(t => t.Count)
            .ThenBy(t => t.FirstIndex)
            .ToList();

        List<string> Pick(bool inResume) =>
            jobTerms.Where(t => t.IsSkill && resumeKeys.Contains(t.Key) == inResume).Select(t => t.Key).ToList();

        return (Pick(true), Pick(false));
    }
}
