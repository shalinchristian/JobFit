using FluentAssertions;
using JobFit.Services;
using Xunit;

public class MatchInsightsBuilderTests
{
    [Fact]
    public void BuildPreview_TruncatesLongText()
    {
        MatchInsightsBuilder.BuildPreview(new string('x', 1000)).Should().HaveLength(703).And.EndWith("...");
        MatchInsightsBuilder.BuildPreview("short").Should().Be("short");
    }

    [Theory]
    [InlineData(90, "move into interview")]
    [InlineData(ScoreBands.Excellent, "move into interview")]
    [InlineData(62, "shortlist")]
    [InlineData(45, "screening round")]
    public void BuildRecommendationSummary_DependsOnScore(double score, string expected)
    {
        MatchInsightsBuilder.BuildRecommendationSummary(score, new List<string>()).Should().Contain(expected);
    }

    [Fact]
    public void BuildInterviewQuestions_ReturnsAtMostTenDistinct()
    {
        var q = MatchInsightsBuilder.BuildInterviewQuestions("python docker sql azure", new[] { "python" }, new[] { "azure" });
        q.Should().HaveCountLessOrEqualTo(10).And.OnlyHaveUniqueItems();
        q.Should().Contain(x => x.Contains("Python")).And.Contain(x => x.Contains("Azure"));
    }
}
