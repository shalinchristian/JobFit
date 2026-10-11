using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using JobFit.Data;
using JobFit.Middleware;
using JobFit.Repositories;
using JobFit;
using JobFit.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

// MVC + API + Swagger
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    options.Filters.Add<TextLengthLimitFilter>();
}).AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "JobFit API",
        Version = "v1",
        Description = "Check a resume against a job posting, get a gap plan and interview prep, and track applications."
    });
});

// Database: SQLite file by default (tracker survives restarts); InMemory for tests/demos.
var provider = builder.Configuration["Database:Provider"] ?? "Sqlite";
var connectionString = builder.Configuration.GetConnectionString("JobFit") ?? "Data Source=jobfit.db";
builder.Services.AddDbContext<AppDbContext>(opt =>
{
    if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
        opt.UseInMemoryDatabase(builder.Configuration["Database:InMemoryName"] ?? "JobFit");
    else
        opt.UseSqlite(connectionString);
});

// Python similarity service (optional: the app falls back to keyword scoring when it is down)
builder.Services.AddHttpClient<ISimilarityClient, SimilarityClient>(client =>
{
    var baseUrl = builder.Configuration["PythonService:BaseUrl"] ?? "http://localhost:5001";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddHttpClient("HealthCheck");

builder.Services.AddScoped<IResumeRepository, ResumeRepository>();
builder.Services.AddScoped<IJobRepository, JobRepository>();
builder.Services.AddScoped<IResumeService, ResumeService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddScoped<IJobTrackerService, JobTrackerService>();
builder.Services.AddHostedService<PythonServiceHealthCheck>();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Create the database on first run, optionally seed demo jobs.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    if (app.Configuration.GetValue<bool>("Demo:Seed"))
        await DemoSeeder.SeedAsync(scope.ServiceProvider);
}

app.UseMiddleware<ExceptionHandlerMiddleware>();
app.UseMiddleware<ApiBodyLimitMiddleware>();
app.UseStaticFiles();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "JobFit API v1");
    c.RoutePrefix = "swagger";
    c.DocumentTitle = "JobFit API";
});

app.UseCors();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "JobFit" }));

app.Run();

/// <summary>Exposed for WebApplicationFactory in integration tests.</summary>
public partial class Program { }
