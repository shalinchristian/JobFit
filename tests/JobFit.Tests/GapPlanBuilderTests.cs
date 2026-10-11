using FluentAssertions;
using JobFit.Models;
using JobFit.Services;
using Xunit;

public class GapPlanBuilderTests
{
    [Fact]
    public void RelatedSkillOnResume_SuggestsShowingProject()
    {
        var plan = GapPlanBuilder.Build(new[] { "azure" }, new[] { "aws" });
        plan.Should().ContainSingle();
        plan[0].Action.Should().Be(GapAction.ShowProject);
        plan[0].Skill.Should().Be("Azure");
        plan[0].Advice.Should().Contain("AWS");
    }

    [Fact]
    public void UnrelatedKnownSkill_SuggestsLearning()
    {
        var plan = GapPlanBuilder.Build(new[] { "kubernetes" }, new[] { "react" });
        plan[0].Action.Should().Be(GapAction.Learn);
        plan[0].Advice.Should().StartWith("Learn Kubernetes:");
    }

    [Fact]
    public void GenericTerm_IsSkipped_SoftSkill_IsReword()
    {
        GapPlanBuilder.Build(new[] { "onboarding" }, Array.Empty<string>()).Should().BeEmpty();
        GapPlanBuilder.Build(new[] { "onboarding", "leadership" }, Array.Empty<string>())
            .Should().ContainSingle().Which.Action.Should().Be(GapAction.Reword);
    }

    [Fact]
    public void KnownSkillsComeFirst_AndCapAtEight()
    {
        var missing = new[] { "onboarding", "budgets", "docker", "kubernetes", "terraform", "redis", "graphql",
                              "jest", "playwright", "tableau", "excel", "rust" };
        var plan = GapPlanBuilder.Build(missing, Array.Empty<string>());
        plan.Should().HaveCount(GapPlanBuilder.DefaultMaxItems);
        plan.Should().OnlyContain(p => p.Action != GapAction.Reword);
        plan[0].Skill.Should().Be("Docker");
    }

    [Fact]
    public void DeduplicatesAliasesAndIgnoresBlanks()
    {
        var plan = GapPlanBuilder.Build(new[] { "postgres", "PostgreSQL", " ", "" }, Array.Empty<string>());
        plan.Should().ContainSingle().Which.Skill.Should().Be("PostgreSQL");
    }

    [Fact]
    public void IsDeterministic() =>
        GapPlanBuilder.Build(new[] { "azure", "kubernetes", "budgets" }, new[] { "aws" })
            .Should().Equal(GapPlanBuilder.Build(new[] { "azure", "kubernetes", "budgets" }, new[] { "aws" }));

    [Fact]
    public void NoMissing_EmptyPlan() =>
        GapPlanBuilder.Build(Array.Empty<string>(), new[] { "python" }).Should().BeEmpty();
}

public class ApplicantSummaryTests
{
    [Theory]
    [InlineData(80, "Strong fit")]
    [InlineData(60, "Good base")]
    [InlineData(40, "Partial fit")]
    [InlineData(10, "Long shot")]
    public void DependsOnScore(double score, string expected) =>
        MatchInsightsBuilder.BuildApplicantSummary(score, new[] { "Azure", "Redis" })
            .Should().StartWith(expected).And.Contain("Azure and Redis");

    [Fact]
    public void SoftSkill_IsReword() =>
        GapPlanBuilder.Build(new[] { "communication" }, Array.Empty<string>())[0].Action.Should().Be(GapAction.Reword);
}
