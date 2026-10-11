namespace JobFit.Services;

public interface ISimilarityClient
{
    /// <summary>Calls the Python microservice and returns a similarity score (0–100).</summary>
    Task<double> GetSimilarityScoreAsync(string text1, string text2);
}
