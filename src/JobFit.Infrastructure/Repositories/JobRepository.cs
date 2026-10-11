using Microsoft.EntityFrameworkCore;
using JobFit.Data;
using JobFit.Models;

namespace JobFit.Repositories;

/// <summary>EF Core implementation of the job tracker store (SQLite in the app, InMemory in tests).</summary>
public class JobRepository : IJobRepository
{
    private readonly AppDbContext _db;

    public JobRepository(AppDbContext db) => _db = db;

    public async Task<JobApplication> AddAsync(JobApplication job)
    {
        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();
        return job;
    }

    public Task<JobApplication?> GetByIdAsync(Guid id) =>
        _db.Jobs.FirstOrDefaultAsync(j => j.Id == id);

    public async Task<List<JobApplication>> ListAsync() =>
        (await _db.Jobs.AsNoTracking().ToListAsync())
            .OrderByDescending(j => j.CreatedAt)
            .ToList();

    public async Task UpdateAsync(JobApplication job)
    {
        _db.Jobs.Update(job);
        await _db.SaveChangesAsync();
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var job = await _db.Jobs.FindAsync(id);
        if (job is null)
            return false;

        _db.Jobs.Remove(job);
        await _db.SaveChangesAsync();
        return true;
    }
}
