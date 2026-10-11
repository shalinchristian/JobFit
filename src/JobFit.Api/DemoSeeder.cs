using JobFit.DTOs;
using JobFit.Models;
using JobFit.Services;

namespace JobFit;

/// <summary>
/// Seeds a few demo jobs (fictional companies, fictional candidate) when "Demo:Seed" is true
/// and the tracker is empty. Scores come from the real matching pipeline.
/// </summary>
public static class DemoSeeder
{
    public const string DemoResume = """
        Alex Rivera - Software Developer
        Summary: Full-stack developer with 3 years building web apps in C#, .NET and React.
        Skills: C#, ASP.NET Core, .NET, React, JavaScript, TypeScript, SQL, PostgreSQL, Docker, Git, GitHub, REST APIs, xUnit, Agile
        Experience:
        - Built a REST API in ASP.NET Core with EF Core and PostgreSQL serving 20k daily requests.
        - Built React dashboards with TypeScript; cut page load time by 40%.
        - Containerized three services with Docker and set up GitHub Actions builds.
        - Wrote xUnit tests that raised coverage from 35% to 80%.
        - Worked in an Agile team of six with two-week sprints; presented demos to stakeholders.
        Projects: Budget tracker (React + ASP.NET Core), weather CLI in Python.
        """;

    private static readonly (string Title, string Company, JobStatus Status, string Posting, int DaysAgo)[] Jobs =
    {
        ("Backend Developer (.NET)", "Northwind Labs", JobStatus.Interview, """
            We are hiring a Backend Developer to build REST APIs in C# and ASP.NET Core.
            You will design PostgreSQL schemas, write xUnit tests, ship with Docker and CI/CD on GitHub,
            and deploy to Azure. Nice to have: Redis, microservices, Kubernetes.
            """, 9),
        ("Full-Stack Engineer", "Bluefin Health", JobStatus.Applied, """
            Full-stack engineer for our patient portal. React and TypeScript on the frontend,
            Node.js and GraphQL on the backend, PostgreSQL, AWS, Jest and Playwright tests.
            Accessibility matters: we follow WCAG. Agile, small product team.
            """, 6),
        ("Platform Engineer", "Cobalt Cloud", JobStatus.Saved, """
            Platform engineer to run our Kubernetes clusters on AWS. Terraform, Linux, Docker,
            Jenkins pipelines, Python scripting, observability with Prometheus and Grafana.
            On-call rotation and incident reviews.
            """, 3),
        ("Junior Data Analyst", "Maple & Co", JobStatus.Rejected, """
            Junior data analyst: SQL, Excel, Tableau dashboards, Python with pandas,
            communicate insights to stakeholders, basic statistics and A/B testing.
            """, 14),
    };

    public static async Task SeedAsync(IServiceProvider services)
    {
        var tracker = services.GetRequiredService<IJobTrackerService>();
        if ((await tracker.ListAsync()).Count > 0)
            return;

        var repo = services.GetRequiredService<JobFit.Repositories.IJobRepository>();
        foreach (var job in Jobs)
        {
            var saved = await tracker.CheckAndSaveAsync(new CreateJobRequest
            {
                Title = job.Title, Company = job.Company, Status = job.Status,
                ResumeText = DemoResume, JobDescription = job.Posting
            });

            // Spread the dates out so the tracker looks like real use.
            var entity = await repo.GetByIdAsync(saved.Id);
            if (entity is not null)
            {
                entity.CreatedAt = DateTime.UtcNow.AddDays(-job.DaysAgo);
                entity.UpdatedAt = DateTime.UtcNow.AddDays(-Math.Max(0, job.DaysAgo - 2));
                await repo.UpdateAsync(entity);
            }
        }
    }
}
