using FluentAssertions;
using JobFit.Services;
using Xunit;

public class FallbackScorerTests
{
    [Fact]
    public void FullCoverage_IsCeiling85_NoCoverage_IsFloor20()
    {
        FallbackScorer.Score("python docker sql", "Python, Docker and SQL").Should().Be(85);
        FallbackScorer.Score("baking bread", "python docker sql").Should().Be(20);
    }

    [Fact]
    public void EmptyJob_IsZero() => FallbackScorer.Score("python", "  ").Should().Be(0);

    [Fact]
    public void SkillsWeighMoreThanFillerWords()
    {
        // job: python (skill, w3) + spreadsheets (generic, w1)
        var hasSkill = FallbackScorer.Score("python", "python spreadsheets");
        var hasGeneric = FallbackScorer.Score("spreadsheets", "python spreadsheets");
        hasSkill.Should().BeGreaterThan(hasGeneric);
        hasSkill.Should().Be(Math.Round(20 + 65 * Math.Sqrt(0.8), 1)); // skills 100% x0.8, words 0% x0.2
    }

    [Fact]
    public void IsDeterministic() =>
        FallbackScorer.Score("c# react sql", "react sql azure kubernetes")
            .Should().Be(FallbackScorer.Score("c# react sql", "react sql azure kubernetes"));
}
