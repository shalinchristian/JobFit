using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using JobFit.Models;
using JobFit.Services;
using JobFit.ViewModels;
using Xunit;
using Moq;

/// <summary>Regression tests for QA round 3 (2.2.0).</summary>
public class CatalogOnlyTests
{
    private const string React = """
        Frontend Engineer (React) - Brightloop, Coimbatore (Remote-friendly)
        Build reusable UI components in React and TypeScript with Next.js. Manage state with Redux and React Query;
        consume REST APIs and GraphQL. Unit tests with Jest and React Testing Library, end-to-end tests with Cypress.
        Ensure accessibility (WCAG) and responsive design using CSS, Sass and Tailwind CSS. Optimise Core Web Vitals;
        work with Webpack and Vite. Collaborate with designers in Figma and ship via CI/CD on Vercel.
        Requirements: JavaScript, React, HTML, CSS, Git and Node.js tooling. Nice to have: Storybook, React Native.
        Great benefits, generous leave, team offsites. Apply today!
        """;

    private const string Data = """
        Data Analyst - Kestrel Retail Group, Leeds (Hybrid). Write complex SQL against Snowflake and BigQuery.
        Dashboards in Power BI and Tableau for stakeholders. Python (pandas, NumPy) and Excel, pivot tables.
        A/B tests and statistical analysis; forecast demand. ETL pipelines with dbt and Airflow.
        Present findings to senior leadership. Nice to have: R, Looker, machine learning basics.
        """;

    [Fact]
    public void ReactPosting_OnlyCatalogSkills()
    {
        var k = KeywordExtractor.Compare("", React).Missing;
        k.Should().OnlyContain(x => SkillCatalog.IsSkill(x));
        k.Should().Contain(new[] { "react", "typescript", "next.js", "redux", "react query", "graphql", "jest",
            "react testing library", "cypress", "accessibility", "wcag", "responsive design", "css", "sass", "tailwind",
            "web performance", "webpack", "vite", "figma", "ci/cd", "vercel", "javascript", "html", "git", "node.js",
            "storybook", "react native" });
    }

    [Fact]
    public void DataPosting_OnlyCatalogSkills()
    {
        var k = KeywordExtractor.Compare("", Data, new[] { "Kestrel Retail Group", "Data Analyst" }).Missing;
        k.Should().OnlyContain(x => SkillCatalog.IsSkill(x));
        k.Should().Contain(new[] { "sql", "snowflake", "bigquery", "power bi", "tableau", "stakeholders", "python",
            "pandas", "numpy", "excel", "a/b testing", "statistics", "forecasting", "etl", "dbt", "airflow",
            "presentation", "r", "looker", "machine learning" });
    }

    [Theory]
    [InlineData("Sass", "sass")]
    [InlineData("SCSS modules", "sass")]
    [InlineData("Sass/SCSS", "sass")]
    public void Sass_IsDetected_AndIsNotCss(string text, string key)
    {
        var k = KeywordExtractor.ExtractKeywords(text);
        k.Should().Contain(key).And.NotContain("css");
    }

    [Fact]
    public void ReactNative_IsNotReact() =>
        KeywordExtractor.ExtractKeywords("React Native").Should().Equal("react native");

    [Fact]
    public void GapPlan_NeverHasRandomWords()
    {
        var (matched, missing) = KeywordExtractor.Compare("Tableau SQL", Data);
        var plan = GapPlanBuilder.Build(missing, matched);
        plan.Should().OnlyContain(p => SkillCatalog.IsSkill(p.Skill));
        plan.Where(p => p.Action == GapAction.Reword)
            .Should().OnlyContain(p => SkillCatalog.Find(p.Skill)!.Category == "soft");
        plan.Single(p => p.Skill == "Power BI").Action.Should().Be(GapAction.ShowProject); // Tableau
        plan.Single(p => p.Skill == "Snowflake").Action.Should().Be(GapAction.ShowProject); // SQL
    }
}

public class InterviewQuestionRound3Tests
{
    [Fact]
    public void MentionsQuestions_SkipAllMatched_AndSoftSkillsAreBehavioural()
    {
        var matched = new[] { "react", "typescript", "redux", "jest", "webpack", "sass", "communication" };
        var missing = new[] { "next.js", "cypress", "vite", "leadership" };
        var q = MatchInsightsBuilder.BuildInterviewQuestions(
            "React TypeScript Redux Jest Webpack Sass Next.js Cypress Vite Storybook Figma communication leadership",
            matched, missing);

        foreach (var m in new[] { "React", "TypeScript", "Redux", "Jest", "Webpack", "Sass/SCSS" })
            q.Should().NotContain(x => x.StartsWith("The posting also mentions " + m + "."));
        q.Should().NotContain(x => x.Contains("project where you used Communication") || x.Contains("used Leadership"));
        q.Should().Contain(x => x.StartsWith("Tell me about a situation where communication"));
        q.Should().Contain(x => x.StartsWith("Tell me about a situation where leadership"));
        q.Should().Contain(x => x.StartsWith("The posting also mentions Storybook."));
        q.Should().HaveCountLessOrEqualTo(10).And.OnlyHaveUniqueItems();
    }
}

public class RoundingTests
{
    [Theory]
    [InlineData(71.6, 72, FitBand.Excellent, "great")]
    [InlineData(71.5, 72, FitBand.Excellent, "great")]
    [InlineData(71.4, 71, FitBand.Good, "good")]
    [InlineData(54.5, 55, FitBand.Good, "good")]
    [InlineData(54.4, 54, FitBand.Moderate, "fair")]
    [InlineData(39.5, 40, FitBand.Moderate, "fair")]
    [InlineData(19.5, 20, FitBand.Weak, "low")]
    [InlineData(19.4, 19, FitBand.Poor, "low")]
    [InlineData(-3, 0, FitBand.Poor, "low")]
    [InlineData(100.4, 100, FitBand.Excellent, "great")]
    public void RoundOnce_DisplayLabelBandToneAgree(double raw, int shown, FitBand band, string tone)
    {
        ScoreBands.Round(raw).Should().Be(shown);
        ScoreBands.For(raw).Should().Be(band);
        ScoreBands.For(shown).Should().Be(band);
        ScoreBands.Tone(raw).Should().Be(tone);
        MatchService.GetMatchLabel(raw).Should().Be($"{band} Match");
        new FitResultViewModel { Score = raw }.ScoreRounded.Should().Be(shown);
        new FitResultViewModel { Score = raw }.Tone.Should().Be(tone);
    }

    [Fact]
    public async Task MatchService_ReturnsTheRoundedScore()
    {
        var sim = new Moq.Mock<ISimilarityClient>();
        sim.Setup(s => s.GetSimilarityScoreAsync(Moq.It.IsAny<string>(), Moq.It.IsAny<string>())).ReturnsAsync(71.6);
        var sut = new MatchService(Moq.Mock.Of<IResumeService>(), sim.Object,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MatchService>.Instance);
        var r = await sut.MatchTextAsync("plain words only", "plain words only"); // no catalog skills -> semantic only
        r.SkillCoverage.Should().BeNull();
        r.MatchScore.Should().Be(72);
        r.MatchLabel.Should().Be("Excellent Match");
    }

    [Fact]
    public void GoodSummary_DoesNotSayStrong()
    {
        MatchInsightsBuilder.BuildDetailedSummary("Ana", 60, new[] { "react" }, new[] { "vue" })
            .Should().NotContain("strong overall fit").And.Contain("good overall fit");
    }
}

public class FallbackCalibrationTests
{
    [Fact]
    public void StaysInModelLikeRange()
    {
        const string job = "C#, ASP.NET Core, SQL, Docker, Kubernetes, Azure, Redis, xUnit, Terraform, CI/CD";
        var strong = FallbackScorer.Score("C# ASP.NET Core SQL Docker Kubernetes Azure Redis xUnit Terraform CI/CD", job);
        var weak = FallbackScorer.Score("Photoshop Illustrator branding", job);
        strong.Should().BeInRange(75, 85);
        weak.Should().BeInRange(15, 30);
    }
}

public class Round3ApiTests : IClassFixture<JobFitAppFactory>
{
    private readonly JobFitAppFactory _factory;
    public Round3ApiTests(JobFitAppFactory factory) => _factory = factory;

    [Fact]
    public async Task OverlongText_Is413()
    {
        var client = _factory.CreateClient();
        var r = await client.PostAsJsonAsync("/api/match/text",
            new { resumeText = new string('a', DocumentTextExtractor.MaxTextChars + 1), jobDescription = "react" });
        r.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        (await r.Content.ReadAsStringAsync()).Should().Contain("ResumeText", "the message names the field").And.Contain("1,000,000").And.Contain("Request is too large");
    }

    [Fact]
    public async Task OverlongContentLength_Is413_BeforeReading()
    {
        var client = _factory.CreateClient();
        var body = new ByteArrayContent(new byte[11 * 1024 * 1024]);
        body.Headers.ContentType = new("application/json");
        var r = await client.PostAsync("/api/jobs", body);
        r.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
    }

    [Fact]
    public async Task EmptyUploadedFile_SaysFileIsEmpty()
    {
        var client = _factory.CreateClient();
        var page = await client.GetStringAsync("/");
        var token = System.Text.RegularExpressions.Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        using var form = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent("Backend Developer"), "JobTitle" },
            { new StringContent("C# and Docker"), "JobDescriptionText" },
            { new ByteArrayContent(Array.Empty<byte>()), "ResumeFile", "empty.txt" }
        };
        var html = await (await client.PostAsync("/Home/Check", form)).Content.ReadAsStringAsync();
        html.Should().Contain("&quot;empty.txt&quot; is empty").And.NotContain("Add a job title, your resume");
    }
}

public class NotSkillPhraseTests
{
    [Fact]
    public void SeniorLeadership_IsAnAudience_NotLeadershipSkill()
    {
        KeywordExtractor.ExtractKeywords("Present findings to senior leadership").Should().NotContain("leadership").And.Contain("presentation");
        KeywordExtractor.ExtractKeywords("Leadership of a small team").Should().Contain("leadership");
    }
}
