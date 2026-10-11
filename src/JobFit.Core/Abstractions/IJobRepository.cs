using JobFit.Models;

namespace JobFit.Repositories;

/// <summary>Persistence contract for tracked jobs.</summary>
public interface IJobRepository
{
    Task<JobApplication> AddAsync(JobApplication job);
    Task<JobApplication?> GetByIdAsync(Guid id);
    Task<List<JobApplication>> ListAsync();
    Task UpdateAsync(JobApplication job);
    Task<bool> DeleteAsync(Guid id);
}
