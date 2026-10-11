using FluentAssertions;
using JobFit.Services;
using Xunit;

public class KeywordExtractorTests
{
    [Fact]
    public void ExtractKeywords_RemovesStopWordsAndShortTokens()
    {
        var k = KeywordExtractor.ExtractKeywords("The C# developer and a SQL expert, with Docker.");
        k.Should().Contain(new[] { "c#", "sql", "expert", "docker" });
        k.Should().NotContain("developer"); // job-title filler since QA round 1
        k.Should().NotContain(new[] { "the", "and", "with", "a" });
    }

    [Fact]
    public void Compare_SplitsMatchedAndMissing()
    {
        var (matched, missing) = KeywordExtractor.Compare("python docker sql", "python kubernetes sql");
        matched.Should().BeEquivalentTo(new[] { "python", "sql" });
        missing.Should().BeEquivalentTo(new[] { "kubernetes" });
    }
}

public class KeywordExtractorSkillTests
{
    [Fact]
    public void KeepsShortKnownSkills_AndDropsPostingFiller()
    {
        var k = KeywordExtractor.ExtractKeywords("We are hiring! You will build REST APIs in C# with a strong team.");
        k.Should().Contain(new[] { "c#", "rest api" });
        k.Should().NotContain(new[] { "hiring", "build", "strong", "team" });
    }

    [Fact]
    public void Compare_RanksKnownSkillsFirst()
    {
        var (_, missing) = KeywordExtractor.Compare("nothing here", "onboarding kubernetes budgets docker");
        missing.Take(2).Should().BeEquivalentTo(new[] { "docker", "kubernetes" });
    }
}
