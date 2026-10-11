namespace JobFit.Services;

/// <summary>
/// Background service that pings the Python microservice at startup
/// and logs a clear warning if it cannot be reached.
///
/// This does NOT block the API from starting - it just alerts developers
/// early so they're not surprised when /api/match returns 503.
/// </summary>
public class PythonServiceHealthCheck : IHostedService
{
    private readonly IHttpClientFactory _factory;
    private readonly IConfiguration _config;
    private readonly ILogger<PythonServiceHealthCheck> _logger;

    public PythonServiceHealthCheck(
        IHttpClientFactory factory,
        IConfiguration config,
        ILogger<PythonServiceHealthCheck> logger)
    {
        _factory = factory;
        _config = config;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        var baseUrl = _config["PythonService:BaseUrl"] ?? "http://localhost:5001";

        _logger.LogInformation("Checking Python similarity service at {Url}...", baseUrl);

        try
        {
            var client = _factory.CreateClient();
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(5);

            var response = await client.GetAsync("/health", cancellationToken);

            if (response.IsSuccessStatusCode)
                _logger.LogInformation("Python similarity service is reachable at {Url}", baseUrl);
            else
                _logger.LogWarning("Python service responded with {Status}. Matching may fail.", response.StatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                "Python similarity service is NOT reachable at {Url}.\n" +
                "Reason: {Error}\n" +
                "Start it with: cd services/nlp && python app.py",
                baseUrl, ex.Message);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
