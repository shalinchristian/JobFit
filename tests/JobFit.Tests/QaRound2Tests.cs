using FluentAssertions;
using JobFit.Models;
using JobFit.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

/// <summary>Regression tests for QA round 2 (items 1-7).</summary>
public class ScoreBandTests
{
    [Theory]
    [InlineData(77, FitBand.Excellent, "Excellent Match", "Strong fit", "move into interview", "great")]
    [InlineData(62, FitBand.Good, "Good Match", "Good base", "shortlist", "good")]
    [InlineData(45, FitBand.Moderate, "Moderate Match", "Partial fit", "screening round", "fair")]
    [InlineData(28, FitBand.Weak, "Weak Match", "Long shot", "strengthen or clarify", "low")]
    [InlineData(10, FitBand.Poor, "Poor Match", "Long shot", "strengthen or clarify", "low")]
    public void LabelSummaryRecommendationAndTone_AllAgree(double score, FitBand band, string label, string summary, string rec, string tone)
    {
        ScoreBands.For(score).Should().Be(band);
        MatchService.GetMatchLabel(score).Should().Be(label);
        MatchInsightsBuilder.BuildApplicantSummary(score, new[] { "Azure" }).Should().StartWith(summary);
        MatchInsightsBuilder.BuildRecommendationSummary(score, new[] { "Azure" }).Should().Contain(rec);
        ScoreBands.Tone(score).Should().Be(tone);
    }

    [Fact]
    public void Boundaries_AreInclusive()
    {
        ScoreBands.For(ScoreBands.Excellent).Should().Be(FitBand.Excellent);
        ScoreBands.For(ScoreBands.Excellent - 0.6).Should().Be(FitBand.Good); // 71.4 rounds to 71
        ScoreBands.For(ScoreBands.Good).Should().Be(FitBand.Good);
        ScoreBands.For(ScoreBands.Moderate).Should().Be(FitBand.Moderate);
        ScoreBands.For(ScoreBands.Weak).Should().Be(FitBand.Weak);
    }
}

public class TokenizerTests
{
    [Theory]
    [InlineData("Built services in ASP.NET Core 8 with EF Core", "asp.net core", "entity framework core")]
    [InlineData("APIs in Node.js and Next.js", "node.js", "next.js")]
    [InlineData("Own CI/CD pipelines", "ci/cd", null)]
    [InlineData("Owned CI-CD in GitHub Actions", "ci/cd", "github actions")]
    [InlineData("Strong C# and .NET 8, SQL Server", "c#", ".net")]
    public void MultiWordAndPunctuatedSkills_StayWhole(string text, string skill1, string? skill2)
    {
        var k = KeywordExtractor.ExtractKeywords(text);
        k.Should().Contain(skill1);
        if (skill2 is not null) k.Should().Contain(skill2);
        k.Should().NotContain(new[] { "asp", "net", "core", "node", "next", "cd", "actions", "framework" });
    }

    [Fact]
    public void AspNetCore_DoesNotAlsoCountAsAspNetOrDotNet()
    {
        var k = KeywordExtractor.ExtractKeywords("ASP.NET Core");
        k.Should().Equal("asp.net core");
    }

    [Fact]
    public void WordBoundaries_AreRespected()
    {
        var k = KeywordExtractor.ExtractKeywords("reactive programming, the rest of the team");
        k.Should().NotContain(new[] { "react", "javascript", "rest api" });
    }

    [Theory]
    [InlineData("apis", "api")]
    [InlineData("designed", "design")]
    [InlineData("dashboards", "dashboard")]
    [InlineData("libraries", "library")]
    [InlineData("caching", "cach")]
    [InlineData("cached", "cach")]
    [InlineData("planned", "plan")]
    public void Normalize_StemsPluralsAndTenses(string word, string stem) =>
        KeywordExtractor.Normalize(word).Should().Be(stem);

    [Fact]
    public void PluralsAndTenses_Match_AcrossResumeAndPosting()
    {
        var (matched, missing) = KeywordExtractor.Compare("Mentored juniors; REST APIs; microservice",
                                                        "mentor others, a REST API, microservices");
        matched.Should().Contain(new[] { "mentoring", "rest api", "microservices" });
        missing.Should().BeEmpty();
        KeywordExtractor.ExtractKeywords("dashboards").Should().Equal(KeywordExtractor.ExtractKeywords("dashboard"));
    }

    [Fact]
    public void PostingFiller_IsDropped()
    {
        var k = KeywordExtractor.ExtractKeywords(
            "Senior Backend Engineer (Hybrid). Design and build. Competitive salary, learning budget, " +
            "clear communication; collaborate with product managers. Apply now! Pune, India.");
        k.Should().NotContain(new[] { "senior", "backend", "engineer", "hybrid", "design", "build", "competitive",
                                      "salary", "learning", "budget", "clear", "collaborate", "product", "managers",
                                      "apply", "pune", "india" });
        k.Should().Contain("communication");
    }

    [Fact]
    public void CompanyAndTitleWords_AreIgnored_ButSkillsInThemAreKept()
    {
        var (_, missing) = KeywordExtractor.Compare("nothing", "Northwind Labs needs logistics and Kubernetes",
            new[] { "Northwind Labs", "Kubernetes Logistics Lead" });
        missing.Should().Contain("kubernetes");
        missing.Should().NotContain(new[] { "northwind", "labs", "logistics" });
    }

    [Fact]
    public void Missing_IsOrderedByImportance_NotAlphabetically()
    {
        var (_, missing) = KeywordExtractor.Compare("nothing",
            "Terraform once. Azure, Azure, Azure everywhere. Kubernetes and Kubernetes. Zebra zebra zebra, apple.");
        missing.Should().Equal("azure", "kubernetes", "terraform"); // skills by frequency; non-skills dropped
    }

    [Fact]
    public void NonCatalogWords_AreNeverReported()
    {
        var job = string.Join(" ", Enumerable.Range(0, 30).Select(i => "widget" + (char)('a' + i % 26) + "x" + i));
        var (matched, missing) = KeywordExtractor.Compare(job, job + " docker");
        matched.Should().BeEmpty();
        missing.Should().Equal("docker");
    }
}

public class RelatedSkillTests
{
    [Theory]
    [InlineData("mysql", "postgresql", true)]
    [InlineData("sql server", "mysql", true)]
    [InlineData("vue", "react", true)]
    [InlineData("gcp", "aws", true)]
    [InlineData("docker", "kubernetes", true)]
    [InlineData("asp.net core", "microservices", false)]
    [InlineData("postgresql", "redis", false)]
    [InlineData("react", "css", false)]
    [InlineData("figma", "communication", false)]
    public void CuratedMap(string have, string missing, bool related) =>
        SkillCatalog.AreRelated(have, missing).Should().Be(related);

    [Fact]
    public void UnrelatedSameCategory_FallsBackToLearn()
    {
        // Before QA: Redis was "Show a project" just because PostgreSQL is also a database.
        GapPlanBuilder.Build(new[] { "redis" }, new[] { "postgresql" })[0].Action.Should().Be(GapAction.Learn);
        GapPlanBuilder.Build(new[] { "microservices" }, new[] { "asp.net" })[0].Action.Should().Be(GapAction.Learn);
    }

    [Fact]
    public void GenericWords_NeverInPlan()
    {
        GapPlanBuilder.Build(new[] { "logistics", "warehousing", "fleet", "routing" }, Array.Empty<string>()).Should().BeEmpty();
    }
}

public class InterviewQuestionTests
{
    [Fact]
    public void OnlyCatalogSkills_BecomeTopics()
    {
        var q = MatchInsightsBuilder.BuildInterviewQuestions(
            "Platform work with clear goals, logistics, Kafka and Terraform.",
            new[] { "c#", "platform", "logistics" }, new[] { "kafka", "clear" });

        q.Should().Contain(x => x.Contains("C#")).And.Contain(x => x.Contains("Kafka")).And.Contain(x => x.Contains("Terraform"));
        q.Should().NotContain(x => x.Contains("platform") || x.Contains("logistics") || x.Contains("clear"));
        q.Should().HaveCountLessOrEqualTo(10).And.OnlyHaveUniqueItems();
    }
}

public class ServiceLayerIgnoreTests
{
    [Fact]
    public async Task MatchTextAsync_AppliesCompanyAndTitleFilter()
    {
        var sim = new Mock<ISimilarityClient>();
        sim.Setup(s => s.GetSimilarityScoreAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(60);
        var sut = new MatchService(Mock.Of<IResumeService>(), sim.Object, NullLogger<MatchService>.Instance);

        var r = await sut.MatchTextAsync("c#", "Northwind Labs wants C# and logistics", new[] { "Northwind Labs", "Logistics Developer" });

        r.MissingKeywords.Should().NotContain(new[] { "northwind", "labs", "logistics" });
        r.MatchLabel.Should().Be("Excellent Match"); // 0.5 x 60 + 0.5 x 100 (C# is the only skill, and it's matched) = 80
    }
}

public class ResumeSkillsTests
{
    [Fact]
    public async Task ResumeOnlySkills_DriveShowProject()
    {
        // The posting never mentions ASP.NET MVC or MySQL, but having them is evidence for ASP.NET Core / PostgreSQL.
        var sim = new Mock<ISimilarityClient>();
        sim.Setup(s => s.GetSimilarityScoreAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(62);
        var sut = new MatchService(Mock.Of<IResumeService>(), sim.Object, NullLogger<MatchService>.Instance);

        var r = await sut.MatchTextAsync("C# ASP.NET MVC, MySQL", "ASP.NET Core and PostgreSQL");
        r.ResumeSkills.Should().Contain(new[] { "asp.net", "mysql", "c#" });

        var plan = GapPlanBuilder.Build(r.MissingKeywords, r.MatchedKeywords.Concat(r.ResumeSkills));
        plan.Should().OnlyContain(p => p.Action == GapAction.ShowProject);
        plan.Select(p => p.Skill).Should().Equal("ASP.NET Core", "PostgreSQL");
    }
}
