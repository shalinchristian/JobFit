using FluentAssertions;
using JobFit.Data;
using JobFit.DTOs;
using JobFit.Models;
using JobFit.Repositories;
using JobFit.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

/// <summary>Regression tests for QA round 4 (2.3.0).</summary>
// Regression tests for QA round 5 (2.4.0).

public class StaleTrackerLabelTests
{
    [Theory]
    [InlineData(72, "Good Match", "Excellent Match")]
    [InlineData(71.6, "Good Match", "Excellent Match")]
    [InlineData(22, "Poor Match", "Weak Match")]
    public async Task OldRows_GetLabelRecomputedOnRead(double storedScore, string storedLabel, string expected)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var job = new JobApplication { Title = "Old 2.2 row", Score = storedScore, Label = storedLabel };
        db.Jobs.Add(job); await db.SaveChangesAsync();
        var sut = new JobTrackerService(new JobRepository(db), Mock.Of<IMatchService>(), NullLogger<JobTrackerService>.Instance);

        (await sut.GetAsync(job.Id))!.Label.Should().Be(expected);
        (await sut.ListAsync()).Single().Label.Should().Be(expected);
    }
}

public class ScoreBlendTests
{
    [Theory]
    [InlineData(63, 18, 26, 73, FitBand.Excellent)] // QA's near-perfect Android resume (was 63 Good)
    [InlineData(81, 23, 24, 89, FitBand.Excellent)] // strong backend
    [InlineData(65, 6, 24, 58, FitBand.Good)]        // medium backend
    [InlineData(28, 1, 24, 24, FitBand.Weak)]        // weak backend stays Weak
    [InlineData(14, 0, 27, 7, FitBand.Poor)]         // unrelated resume on the SRE posting
    public void Blend_MatchesQaExpectations(double semantic, int matched, int total, int shown, FitBand band)
    {
        var score = ScoreBlend.Blend(semantic, matched, total);
        ScoreBands.Round(score).Should().Be(shown);
        ScoreBands.For(score).Should().Be(band);
    }

    [Fact]
    public void NoCatalogSkills_UsesSemanticOnly() => ScoreBlend.Blend(66.6, 0, 0).Should().Be(66.6);

    [Fact]
    public async Task Fallback_IsBlendedTheSameWay()
    {
        var sim = new Mock<ISimilarityClient>();
        sim.Setup(s => s.GetSimilarityScoreAsync(It.IsAny<string>(), It.IsAny<string>())).ThrowsAsync(new HttpRequestException());
        var sut = new MatchService(Mock.Of<IResumeService>(), sim.Object, NullLogger<MatchService>.Instance);
        var r = await sut.MatchTextAsync("Python, Docker", "Python, Docker, Kubernetes and Terraform");
        r.UsedFallback.Should().BeTrue();
        r.SkillCoverage.Should().Be(50);
        r.MatchScore.Should().Be(ScoreBands.Round(ScoreBlend.Blend(r.SemanticScore, 2, 4)));
    }
}

public class CatalogRound5Tests
{
    private static List<string> Skills(string text) =>
        KeywordExtractor.ExtractTerms(text).Where(t => t.IsSkill).Select(t => t.Key).ToList();

    [Theory]
    [InlineData("Written in C++ with CMake.")]
    [InlineData("Strong C++17 not required, but cpp experience helps")]
    public void CPlusPlus_IsASkill(string text) => Skills(text).Should().Contain("c++").And.NotContain("c");

    [Fact]
    public void CSharp_And_CPlusPlus_StayDistinct() =>
        Skills("We use C#, C++ and C for firmware programming.").Should().Contain(new[] { "c#", "c++", "c" });

    [Fact]
    public void RubyOnRails_NamesRubyAndRails() => Skills("Backend in Ruby on Rails.").Should().Contain(new[] { "ruby", "rails" });

    [Fact]
    public void Rails_ImpliesRuby_OnResume()
    {
        var (matched, _) = KeywordExtractor.Compare("Built apps with Rails and PostgreSQL", "We need Ruby and PostgreSQL");
        matched.Should().Contain("ruby");
    }

    [Fact]
    public void Python_IsNoLongerEvidenceForGo() => SkillCatalog.AreRelated("python", "golang").Should().BeFalse();
}

public class AmbiguousRound5Tests
{
    private static List<string> Skills(string text) =>
        KeywordExtractor.ExtractTerms(text).Where(t => t.IsSkill).Select(t => t.Key).ToList();

    [Theory]
    [InlineData("Swift turnaround on bug fixes is expected.", "swift")]
    [InlineData("We ship fixes fast. Swift turnaround matters here.", "swift")]
    [InlineData("React quickly to customer incidents.", "react")]
    [InlineData("You must react quickly when alerts fire.", "react")]
    [InlineData("All parts are Rust-free and coated.", "rust")]
    [InlineData("We are a Series C startup. Python is our main tool.", "c")]
    [InlineData("You'll work with Ruby on our team, she leads QA.", "ruby")]
    [InlineData("We sponsor a Spring boot camp for new grads.", "spring boot")]
    [InlineData("We sponsor a Spring boot camp for new grads.", "spring")]
    public void FalsePositives_AreGone(string text, string key) => Skills(text).Should().NotContain(key);

    [Theory]
    [InlineData("Swift and SwiftUI for our iOS app.", "swift")]
    [InlineData("Experience with Swift is a plus.", "swift")]
    [InlineData("Rust services handle payments.", "rust")]
    [InlineData("We use React and TypeScript.", "react")]
    [InlineData("Languages: C, Python and Rust.", "c")]
    [InlineData("Ruby, Go and Python on the backend.", "ruby")]
    [InlineData("Java with Spring Boot microservices.", "spring boot")]
    public void RealUses_StillMatch(string text, string key) => Skills(text).Should().Contain(key);
}
