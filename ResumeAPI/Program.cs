using Microsoft.EntityFrameworkCore;
using ResumeAPI.Data;
using ResumeAPI.Middleware;
using ResumeAPI.Repositories;
using ResumeAPI.Services;

var builder = WebApplication.CreateBuilder(args);

// --------------------------------------------------------------
// 1. Logging (Clean for local + Docker)
// --------------------------------------------------------------
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

// --------------------------------------------------------------
// 2. Register MVC + API + Swagger
// --------------------------------------------------------------
builder.Services.AddControllersWithViews(options =>
{
    // Prevent unrelated form fields from being treated as implicitly required.
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Resume Job Matching API",
        Version = "v1",
        Description = "Matches resumes with job descriptions using semantic similarity"
    });
});

// --------------------------------------------------------------
// 3. Database (In-Memory for simplicity)
// --------------------------------------------------------------
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseInMemoryDatabase("ResumeDb"));

// --------------------------------------------------------------
// 4. Python Microservice HttpClient
// --------------------------------------------------------------
builder.Services.AddHttpClient<ISimilarityClient, SimilarityClient>(client =>
{
    var baseUrl = builder.Configuration["PythonService:BaseUrl"]
                  ?? "http://python-service:5001"; // Docker-friendly

    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

// Health check client
builder.Services.AddHttpClient("HealthCheck");

// --------------------------------------------------------------
// 5. Dependency Injection (Core Services)
// --------------------------------------------------------------
builder.Services.AddScoped<IResumeRepository, ResumeRepository>();
builder.Services.AddScoped<IResumeService, ResumeService>();
builder.Services.AddScoped<IMatchService, MatchService>();

builder.Services.AddHostedService<PythonServiceHealthCheck>();

// --------------------------------------------------------------
// 6. CORS (safe for local dev)
// --------------------------------------------------------------
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

// --------------------------------------------------------------
// 7. Middleware Pipeline
// --------------------------------------------------------------

// Global error handler
app.UseMiddleware<ExceptionHandlerMiddleware>();

// HTTPS (safe even in Docker)
app.UseHttpsRedirection();

// Static files (for MVC views, CSS, JS)
app.UseStaticFiles();

// Swagger (ALWAYS enabled for demo)
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Resume Matching API v1");
    c.RoutePrefix = "swagger"; // http://localhost:5000/swagger
});

app.UseCors();
app.UseAuthorization();

// --------------------------------------------------------------
// 8. Routing
// --------------------------------------------------------------

// API Controllers
app.MapControllers();

// MVC Views
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Fallback for SPA-like behavior (optional but nice)
app.MapFallbackToController("Index", "Home");

// Health endpoint
app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    service = "ResumeAPI"
}));

// --------------------------------------------------------------
// 9. Startup Info (debug-friendly)
// --------------------------------------------------------------
Console.WriteLine($"🚀 App running in: {app.Environment.EnvironmentName}");

app.Run();
