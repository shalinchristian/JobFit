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

public class JobTrackerTests
{
    private static AppDbContext NewDb(string? name = null) =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(name ?? Guid.NewGuid().ToString()).Options);

    private static JobTrackerService Sut(AppDbContext db, IMatchService? match = null) =>
        new(new JobRepository(db), match ?? Mock.Of<IMatchService>(), NullLogger<JobTrackerService>.Instance);

    private static SaveJobRequest Req(string title = "Backend Developer") => new()
    {
        Title = title, Company = "Northwind Labs", Score = 72.345, Label = "Good Match",
        JobDescription = "C# docker azure", MatchedSkills = new() { "c#", "docker", "docker" }, MissingSkills = new() { "azure" }
    };

    [Fact]
    public async Task Repository_RoundTripsListsAndStatus()
    {
        var name = Guid.NewGuid().ToString();
        Guid id;
        await using (var db = NewDb(name))
        {
            var job = await new JobRepository(db).AddAsync(new JobApplication
            {
                Title = "QA", Company = "Acme", MatchedSkills = new() { "selenium" }, MissingSkills = new() { "playwright", "jest" },
                Status = JobStatus.Applied
            });
            id = job.Id;
        }
        await using (var db = NewDb(name))
        {
            var loaded = await new JobRepository(db).GetByIdAsync(id);
            loaded!.MissingSkills.Should().Equal("playwright", "jest");
            loaded.Status.Should().Be(JobStatus.Applied);
        }
    }

    [Fact]
    public async Task Save_TrimsRoundsAndDedupes_AndBuildsPlanAndQuestions()
    {
        await using var db = NewDb();
        var saved = await Sut(db).SaveAsync(Req());
        saved.Score.Should().Be(72); // rounded once (ScoreBands.Round)
        saved.MatchedSkills.Should().Equal("c#", "docker");
        saved.Status.Should().Be(JobStatus.Saved);
        saved.GapPlan.Should().ContainSingle(g => g.Skill == "Azure");
        saved.InterviewQuestions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Save_RequiresTitle()
    {
        await using var db = NewDb();
        await Sut(db).Invoking(s => s.SaveAsync(Req(" "))).Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task List_IsNewestFirst()
    {
        await using var db = NewDb();
        var sut = Sut(db);
        await sut.SaveAsync(Req("First"));
        await Task.Delay(5);
        await sut.SaveAsync(Req("Second"));
        (await sut.ListAsync()).Select(j => j.Title).Should().Equal("Second", "First");
    }

    [Fact]
    public async Task UpdateStatus_And_Delete()
    {
        await using var db = NewDb();
        var sut = Sut(db);
        var job = await sut.SaveAsync(Req());

        (await sut.UpdateStatusAsync(job.Id, JobStatus.Offer))!.Status.Should().Be(JobStatus.Offer);
        (await sut.GetAsync(job.Id))!.Status.Should().Be(JobStatus.Offer);
        (await sut.UpdateStatusAsync(Guid.NewGuid(), JobStatus.Applied)).Should().BeNull();
        await sut.Invoking(s => s.UpdateStatusAsync(job.Id, (JobStatus)99)).Should().ThrowAsync<ArgumentException>();

        (await sut.DeleteAsync(job.Id)).Should().BeTrue();
        (await sut.DeleteAsync(job.Id)).Should().BeFalse();
        (await sut.GetAsync(job.Id)).Should().BeNull();
    }

    [Fact]
    public async Task CheckAndSave_UsesMatchService()
    {
        await using var db = NewDb();
        var match = new Mock<IMatchService>();
        match.Setup(m => m.MatchTextAsync("resume", "job", It.IsAny<IEnumerable<string>>())).ReturnsAsync(new MatchResponseDto
        {
            MatchScore = 64, MatchLabel = "Good Match", MatchedKeywords = new() { "react" }, MissingKeywords = new() { "vue" }, UsedFallback = true
        });

        var job = await Sut(db, match.Object).CheckAndSaveAsync(new CreateJobRequest
        {
            Title = "Frontend Dev", Company = "Bluefin", ResumeText = "resume", JobDescription = "job", Status = JobStatus.Applied
        });

        job.Score.Should().Be(64);
        job.UsedFallback.Should().BeTrue();
        job.Status.Should().Be(JobStatus.Applied);
        job.GapPlan.Single().Action.Should().Be(GapAction.ShowProject); // vue <- react (same category)
    }
}
