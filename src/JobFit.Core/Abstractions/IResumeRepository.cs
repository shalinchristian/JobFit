using JobFit.Models;

namespace JobFit.Repositories;

/// <summary>
/// Contract for resume data access operations.
/// Allows swapping in-memory DB with a real DB without changing service layer.
/// </summary>
public interface IResumeRepository
{
    Task<Resume> AddAsync(Resume resume);
    Task<Resume?> GetByIdAsync(Guid id);
    Task<IEnumerable<Resume>> GetAllAsync();
}
