using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using JobFit.DTOs;
using JobFit.Models;
using JobFit.Services;
using Xunit;

public class MatchServiceTests
{
    private readonly Mock<IResumeService> _resumes = new();
    private readonly Mock<ISimilarityClient> _similarity = new();
    private MatchService Sut() => new(_resumes.Object, _similarity.Object, NullLogger<MatchService>.Instance);

    [Fact]
    public async Task MatchAsync_ReturnsScoreLabelAndKeywords()
    {
        var id = Guid.NewGuid();
        _resumes.Setup(r => r.GetResumeByIdAsync(id)).ReturnsAsync(new Resume { Id = id, Content = "python sql docker" });
        _similarity.Setup(s => s.GetSimilarityScoreAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(85);

        var result = await Sut().MatchAsync(new MatchRequestDto { ResumeId = id, JobDescription = "python kubernetes" });

        result.SemanticScore.Should().Be(85);
        result.SkillCoverage.Should().Be(50);
        result.MatchScore.Should().Be(ScoreBands.Round(0.5 * 85 + 0.5 * 100 * Math.Sqrt(0.5))); // 78
        result.MatchLabel.Should().Be("Excellent Match");
        result.MatchedKeywords.Should().Contain("python");
        result.MissingKeywords.Should().Contain("kubernetes");
    }

    [Fact]
    public async Task MatchAsync_EmptyJobDescription_Throws()
    {
        await Sut().Invoking(s => s.MatchAsync(new MatchRequestDto { ResumeId = Guid.NewGuid(), JobDescription = " " }))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task MatchAsync_UnknownResume_Throws()
    {
        _resumes.Setup(r => r.GetResumeByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Resume?)null);
        await Sut().Invoking(s => s.MatchAsync(new MatchRequestDto { ResumeId = Guid.NewGuid(), JobDescription = "x" }))
            .Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task MatchAsync_PythonServiceDown_FallsBackToKeywordScore()
    {
        // JobFit change: a down Python service no longer fails the request; the C# keyword score is used instead.
        _resumes.Setup(r => r.GetResumeByIdAsync(It.IsAny<Guid>())).ReturnsAsync(new Resume { Content = "python docker sql" });
        _similarity.Setup(s => s.GetSimilarityScoreAsync(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new HttpRequestException());

        var result = await Sut().MatchAsync(new MatchRequestDto { ResumeId = Guid.NewGuid(), JobDescription = "python kubernetes" });

        result.UsedFallback.Should().BeTrue();
        result.SemanticScore.Should().Be(FallbackScorer.Score("python docker sql", "python kubernetes"));
        result.MatchScore.Should().Be(ScoreBands.Round(ScoreBlend.Blend(result.SemanticScore, 1, 2)));
        result.MatchScore.Should().BeInRange(1, 99);
        result.MissingKeywords.Should().Contain("kubernetes");
    }

    [Theory]
    [InlineData(typeof(TaskCanceledException))]
    [InlineData(typeof(InvalidOperationException))]
    public async Task MatchTextAsync_TimeoutOrBadResponse_FallsBack(Type exceptionType)
    {
        _similarity.Setup(s => s.GetSimilarityScoreAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync((Exception)Activator.CreateInstance(exceptionType)!);

        var result = await Sut().MatchTextAsync("react typescript", "react typescript");

        result.UsedFallback.Should().BeTrue();
        result.SemanticScore.Should().Be(85); // fallback ceiling
        result.MatchScore.Should().Be(93);    // 0.5 x 85 + 0.5 x 100 (full coverage)
        result.MatchLabel.Should().Be("Excellent Match");
    }

    [Fact]
    public async Task MatchTextAsync_PythonUp_DoesNotUseFallback()
    {
        _similarity.Setup(s => s.GetSimilarityScoreAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(150);
        var result = await Sut().MatchTextAsync("react", "react vue");
        result.UsedFallback.Should().BeFalse();
        result.SemanticScore.Should().Be(100); // clamped
        result.MatchScore.Should().Be(85);     // 0.5 x 100 + 0.5 x 100 x sqrt(1/2) = 85.4
    }
}
