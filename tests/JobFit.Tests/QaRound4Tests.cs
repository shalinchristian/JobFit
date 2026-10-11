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
public class ServerSideLabelTests
{
    [Theory]
    [InlineData(71.5, "Good Match", 72, "Excellent Match")]
    [InlineData(54.4, "Excellent Match", 54, "Moderate Match")]
    [InlineData(90, "Totally Fake", 90, "Excellent Match")]
    public async Task Save_IgnoresClientLabel_AndRounds(double score, string postedLabel, double savedScore, string savedLabel)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var sut = new JobTrackerService(new JobRepository(db), Mock.Of<IMatchService>(), NullLogger<JobTrackerService>.Instance);
        var saved = await sut.SaveAsync(new SaveJobRequest { Title = "Dev", Score = score, Label = postedLabel });
        saved.Score.Should().Be(savedScore);
        saved.Label.Should().Be(savedLabel);
        (await db.Jobs.SingleAsync()).Label.Should().Be(savedLabel);
    }
}

public class AmbiguousSkillTests
{
    private static HashSet<string> K(string text) => KeywordExtractor.ExtractKeywords(text);

    [Theory]
    [InlineData("Automate toil with Python, Go and Bash")]
    [InlineData("Services written in Golang")]
    [InlineData("golang microservices")]
    [InlineData("Experience with Go.")]
    public void Go_True(string t) => K(t).Should().Contain("golang");

    [Theory]
    [InlineData("You need to go the extra mile")]
    [InlineData("Help us go live in May")]
    [InlineData("Your go-to person for tooling")]
    [InlineData("Go through the backlog weekly")]
    [InlineData("Ready to GO")]
    [InlineData("on the go")]
    public void Go_False(string t) => K(t).Should().NotContain("golang");

    [Theory]
    [InlineData("Nice to have: R, Looker")]
    [InlineData("Analysis in R and Python")]
    [InlineData("Skills: SQL, R")]
    public void R_True(string t) => K(t).Should().Contain("r");

    [Theory]
    [InlineData("Our R&D team")]
    [InlineData("R & D budget")]
    [InlineData("Contact John R. Smith")]
    [InlineData("we're hiring")]
    [InlineData("an r value")]
    public void R_False(string t) => K(t).Should().NotContain("r");

    [Theory]
    [InlineData("Embedded C and C++ programming")]
    [InlineData("Languages: C, Python, Rust")]
    public void C_True(string t) => K(t).Should().Contain("c");

    [Theory]
    [InlineData("Report to C-level executives")]
    [InlineData("We raised a Series C")]
    [InlineData("Strong C# skills")]
    [InlineData("Modern C++ only")]
    [InlineData("Grade C or above in maths")]
    public void C_False(string t) => K(t).Should().NotContain("c");

    [Theory]
    [InlineData("iOS (Swift) team", "swift", true)]
    [InlineData("SwiftUI apps", "swift", true)]
    [InlineData("swift delivery of features", "swift", false)]
    [InlineData("Rust services", "rust", true)]
    [InlineData("rust-proof coatings", "rust", false)]
    [InlineData("Flutter and Dart", "dart", true)]
    [InlineData("a dart board in the office", "dart", false)]
    [InlineData("DI with Hilt/Dagger", "hilt", true)]
    [InlineData("DI with Hilt/Dagger", "dagger", true)]
    [InlineData("up to the hilt", "hilt", false)]
    public void CapitalisedOnly(string t, string key, bool expected) => K(t).Contains(key).Should().Be(expected);

    [Theory]
    [InlineData("REST APIs in Express.js", true)]
    [InlineData("Node.js with Express", true)]
    [InlineData("ExpressJS middleware", true)]
    [InlineData("Express your ideas clearly", false)]
    [InlineData("express delivery", false)]
    public void Express(string t, bool expected) => K(t).Contains("express").Should().Be(expected);

    [Theory]
    [InlineData("Java with Spring and Hibernate", true)]
    [InlineData("Spring Framework experience", true)]
    [InlineData("Starting in Spring 2027", false)]
    [InlineData("spring in your step", false)]
    public void Spring(string t, bool expected) => K(t).Contains("spring").Should().Be(expected);

    [Fact]
    public void SpringBoot_StillWorks() => K("Spring Boot APIs").Should().Contain("spring boot");

    [Theory]
    [InlineData("Python and Excel, including pivot tables", true)]
    [InlineData("Advanced Excel", true)]
    [InlineData("You excel at communication", false)]
    [InlineData("Excel in a fast-paced team", false)]
    [InlineData("people who excel", false)]
    public void Excel(string t, bool expected) => K(t).Contains("excel").Should().Be(expected);

    [Theory]
    [InlineData("Use Coroutines and Flow for async work", true)]
    [InlineData("Kotlin Flow and StateFlow", true)]
    [InlineData("Improve the checkout flow", false)]
    [InlineData("Cash Flow forecasting", false)]
    public void Flow(string t, bool expected) => K(t).Contains("flow").Should().Be(expected);

    [Theory]
    [InlineData("Room for local storage; Retrofit for networking", true)]
    [InlineData("Kotlin, Coroutines, Room, Retrofit", true)]
    [InlineData("Room to grow your career", false)]
    [InlineData("there is room for improvement", false)]
    public void Room(string t, bool expected) => K(t).Contains("room").Should().Be(expected);
}

public class ImpliedSkillTests
{
    [Theory]
    [InlineData("Jenkins", "ci/cd")]
    [InlineData("GitHub Actions", "ci/cd")]
    [InlineData("GitLab CI", "ci/cd")]
    [InlineData("CircleCI", "ci/cd")]
    [InlineData("Azure DevOps", "ci/cd")]
    [InlineData("ASP.NET Core", ".net")]
    [InlineData("ASP.NET MVC", ".net")]
    [InlineData("EF Core", ".net")]
    [InlineData("Ran EKS clusters", "kubernetes")]
    [InlineData("Ran EKS clusters", "aws")]
    [InlineData("AKS", "kubernetes")]
    [InlineData("GKE", "kubernetes")]
    public void SpecificTool_SatisfiesUmbrella(string resume, string umbrella)
    {
        var (matched, missing) = KeywordExtractor.Compare(resume, "We need " + SkillCatalog.Display(umbrella));
        matched.Should().Contain(umbrella);
        missing.Should().NotContain(umbrella);
    }

    [Fact]
    public void ReactNative_DoesNotImplyReact() =>
        KeywordExtractor.Compare("React Native", "React").Missing.Should().Contain("react");

    [Fact]
    public void Umbrella_DoesNotImplySpecific() =>
        KeywordExtractor.Compare("CI/CD", "Jenkins").Missing.Should().Contain("jenkins");
}

public class AndroidAndSreCatalogTests
{
    private const string Android = """
        Build features in Kotlin with Jetpack Compose and MVVM architecture
        Use Coroutines and Flow for async work; Room for local storage; Retrofit for networking
        Dependency injection with Hilt/Dagger. Unit tests with JUnit and Mockito and UI tests with Espresso
        Publish releases through Google Play Console; monitor crashes with Firebase Crashlytics
        Collaborate closely with iOS (Swift) and backend teams. Requirements: Android, Kotlin, Jetpack, Git, Gradle.
        Nice to have: Java, Kotlin Multiplatform, accessibility.
        """;

    [Fact]
    public void AndroidPosting_FindsAllSkills()
    {
        KeywordExtractor.ExtractKeywords(Android).Should().Contain(new[] { "kotlin", "jetpack compose", "mvvm", "coroutines",
            "flow", "room", "retrofit", "hilt", "dagger", "junit", "mockito", "espresso", "google play", "firebase", "ios",
            "swift", "android", "jetpack", "git", "gradle", "java", "kotlin multiplatform", "accessibility" });
    }

    [Fact]
    public void AndroidGaps_UseRelatedPairs_AndMobileTips()
    {
        var (matched, missing) = KeywordExtractor.Compare("Java, XML views, Dagger, Android, Kotlin", Android);
        var plan = GapPlanBuilder.Build(missing, matched, 30);
        plan.Single(p => p.Skill == "Jetpack Compose").Action.Should().Be(GapAction.ShowProject);
        plan.Single(p => p.Skill == "Hilt").Advice.Should().Contain("Dagger");
        plan.Should().NotContain(p => p.Advice.Contains("Lighthouse"));
        plan.Single(p => p.Skill == "Accessibility").Advice.Should().Contain("Accessibility Scanner");
    }

    [Fact]
    public void WebPosting_KeepsLighthouseTip() =>
        GapPlanBuilder.Build(new[] { "accessibility" }, new[] { "react", "css" })[0].Advice.Should().Contain("Lighthouse");

    [Fact]
    public void SreTerms_AreSkills()
    {
        KeywordExtractor.ExtractKeywords("""
            Monitoring with Prometheus, Grafana and PagerDuty; define SLOs and error budgets. Lead incident response,
            blameless postmortems. On-call experience. Istio service mesh, ELK stack, tune Nginx and load balancers,
            networking (TCP/IP, DNS), chaos engineering, Kubernetes (EKS).
            """).Should().Contain(new[] { "prometheus", "grafana", "pagerduty", "slos", "incident response", "on-call",
            "istio", "elk", "nginx", "load balancing", "networking", "chaos engineering", "kubernetes", "eks" });
    }
}

public class NetworkingPhraseTests
{
    [Theory]
    [InlineData("Retrofit for networking", false)]
    [InlineData("Attend networking events", false)]
    [InlineData("strong networking (TCP/IP, DNS)", true)]
    public void Networking(string t, bool expected) => KeywordExtractor.ExtractKeywords(t).Contains("networking").Should().Be(expected);
}
