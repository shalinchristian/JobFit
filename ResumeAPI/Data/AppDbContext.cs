using Microsoft.EntityFrameworkCore;
using ResumeAPI.Models;

namespace ResumeAPI.Data;

/// <summary>
/// EF Core DbContext using an in-memory database.
/// No PostgreSQL setup required — data lives in RAM for the app's lifetime.
/// Swap out UseInMemoryDatabase for UseNpgsql to connect to real PostgreSQL.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    /// <summary>Resumes table (in-memory).</summary>
    public DbSet<Resume> Resumes => Set<Resume>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure the Resume entity
        modelBuilder.Entity<Resume>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.CandidateName).IsRequired().HasMaxLength(200);
            entity.Property(r => r.Content).IsRequired();
        });
    }
}
