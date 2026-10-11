using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using JobFit.DTOs;
using JobFit.Models;
using JobFit.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

/// <summary>Boots the real app in memory with the InMemory DB and a Python client that is "down".</summary>
public class JobFitAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Database:Provider", "InMemory");
        builder.UseSetting("Database:InMemoryName", "api-tests-" + Guid.NewGuid());
        builder.UseSetting("PythonService:BaseUrl", "http://127.0.0.1:9");
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ISimilarityClient>();
            services.AddSingleton<ISimilarityClient, DownSimilarityClient>();
        });
    }

    private sealed class DownSimilarityClient : ISimilarityClient
    {
        public Task<double> GetSimilarityScoreAsync(string text1, string text2) =>
            throw new HttpRequestException("Connection refused (test)");
    }
}

public class JobsApiTests : IClassFixture<JobFitAppFactory>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web) { Converters = { new JsonStringEnumConverter() } };
    private readonly HttpClient _client;

    public JobsApiTests(JobFitAppFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task Create_List_Get_UpdateStatus_Delete()
    {
        var create = await _client.PostAsJsonAsync("/api/jobs", new CreateJobRequest
        {
            Title = "Backend Developer", Company = "Northwind Labs",
            ResumeText = "C# developer with ASP.NET Core, SQL and Docker",
            JobDescription = "Backend role: C#, ASP.NET Core, SQL, Docker, Azure and Kubernetes"
        }, Json);
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var job = (await create.Content.ReadFromJsonAsync<JobDetailDto>(Json))!;
        job.UsedFallback.Should().BeTrue("the Python service is down in this test");
        job.Score.Should().BeInRange(1, 99);
        job.MissingSkills.Should().Contain(new[] { "azure", "kubernetes" });
        job.GapPlan.Should().NotBeEmpty();

        var list = await _client.GetFromJsonAsync<List<JobSummaryDto>>("/api/jobs", Json);
        list.Should().Contain(j => j.Id == job.Id);

        var patch = await _client.PatchAsJsonAsync($"/api/jobs/{job.Id}/status", new { status = "Interview" });
        patch.StatusCode.Should().Be(HttpStatusCode.OK);
        (await _client.GetFromJsonAsync<JobDetailDto>($"/api/jobs/{job.Id}", Json))!.Status.Should().Be(JobStatus.Interview);

        (await _client.DeleteAsync($"/api/jobs/{job.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.GetAsync($"/api/jobs/{job.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Create_WithoutTitle_Is400()
    {
        var r = await _client.PostAsJsonAsync("/api/jobs", new { title = "", resumeText = "react", jobDescription = "react" });
        r.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UnknownStatus_Is400_UnknownJob_Is404()
    {
        (await _client.PatchAsJsonAsync($"/api/jobs/{Guid.NewGuid()}/status", new { status = "Offer" }))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await _client.PatchAsJsonAsync($"/api/jobs/{Guid.NewGuid()}/status", new { status = "Hired" }))
            .StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task MatchText_FallsBackWhenPythonDown()
    {
        var r = await _client.PostAsJsonAsync("/api/match/text", new { resumeText = "react typescript css", jobDescription = "react typescript graphql" });
        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await r.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("usedFallback").GetBoolean().Should().BeTrue();
        body.GetProperty("gapPlan").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Pages_And_Swagger_Render()
    {
        (await _client.GetStringAsync("/")).Should().Contain("Are you ready to apply?");
        (await _client.GetStringAsync("/Tracker")).Should().Contain("Your applications");
        (await _client.GetStringAsync("/swagger/v1/swagger.json")).Should().Contain("/api/jobs/{id}/status");
        (await _client.GetAsync("/health")).StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public class UploadUiTests : IClassFixture<JobFitAppFactory>
{
    private readonly JobFitAppFactory _factory;
    public UploadUiTests(JobFitAppFactory factory) => _factory = factory;

    [Fact]
    public async Task OversizedResume_GetsFriendlyMessage_NotConnectionReset()
    {
        var client = _factory.CreateClient();
        var page = await client.GetStringAsync("/");
        var token = System.Text.RegularExpressions.Regex.Match(page, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        token.Should().NotBeEmpty();

        using var form = new MultipartFormDataContent
        {
            { new StringContent(token), "__RequestVerificationToken" },
            { new StringContent("Backend Developer"), "JobTitle" },
            { new StringContent("C# and Docker"), "JobDescriptionText" },
            { new ByteArrayContent(new byte[16 * 1024 * 1024]), "ResumeFile", "huge.txt" }
        };
        var r = await client.PostAsync("/Home/Check", form);

        r.StatusCode.Should().Be(HttpStatusCode.OK);
        var html = await r.Content.ReadAsStringAsync();
        html.Should().Contain("is too large").And.Contain("10 MB");
        page.Should().Contain("data-max-bytes=\"" + JobFit.Services.DocumentTextExtractor.MaxFileBytes + "\"")
            .And.Contain(JobFit.Services.DocumentTextExtractor.HumanTypes);
    }
}
