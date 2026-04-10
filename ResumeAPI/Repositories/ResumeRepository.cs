using Microsoft.EntityFrameworkCore;
using ResumeAPI.Data;
using ResumeAPI.Models;

namespace ResumeAPI.Repositories;

// ──────────────────────────────────────────────
// Interface — defines the data access contract
// ──────────────────────────────────────────────

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

// ──────────────────────────────────────────────
// Implementation — EF Core with in-memory store
// ──────────────────────────────────────────────

/// <summary>
/// Concrete implementation using EF Core (in-memory or PostgreSQL).
/// </summary>
public class ResumeRepository : IResumeRepository
{
    private readonly AppDbContext _db;

    public ResumeRepository(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>Persists a new resume and returns the saved entity.</summary>
    public async Task<Resume> AddAsync(Resume resume)
    {
        _db.Resumes.Add(resume);
        await _db.SaveChangesAsync();
        return resume;
    }

    /// <summary>Fetches a resume by its unique ID. Returns null if not found.</summary>
    public async Task<Resume?> GetByIdAsync(Guid id)
    {
        return await _db.Resumes.FindAsync(id);
    }

    /// <summary>Returns all stored resumes.</summary>
    public async Task<IEnumerable<Resume>> GetAllAsync()
    {
        return await _db.Resumes.ToListAsync();
    }
}
