using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using JobFit.Models;

namespace JobFit.Data;

/// <summary>
/// EF Core context. The app uses SQLite (jobfit.db) by default so the tracker survives restarts;
/// tests and "Database:Provider=InMemory" use the in-memory provider.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<JobApplication> Jobs => Set<JobApplication>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Resume>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.CandidateName).IsRequired().HasMaxLength(200);
            entity.Property(r => r.Content).IsRequired();
        });

        var listComparer = new ValueComparer<List<string>>(
            (a, b) => a!.SequenceEqual(b!),
            v => v.Aggregate(0, (h, s) => HashCode.Combine(h, s.GetHashCode())),
            v => v.ToList());

        modelBuilder.Entity<JobApplication>(entity =>
        {
            entity.ToTable("Jobs");
            entity.HasKey(j => j.Id);
            entity.Property(j => j.Title).IsRequired().HasMaxLength(200);
            entity.Property(j => j.Company).IsRequired().HasMaxLength(200);
            entity.Property(j => j.Label).HasMaxLength(50);
            entity.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
            entity.Property(j => j.MatchedSkills)
                .HasConversion(v => string.Join('\n', v), v => SplitLines(v))
                .Metadata.SetValueComparer(listComparer);
            entity.Property(j => j.MissingSkills)
                .HasConversion(v => string.Join('\n', v), v => SplitLines(v))
                .Metadata.SetValueComparer(listComparer);
            entity.Property(j => j.ResumeSkills)
                .HasConversion(v => string.Join('\n', v), v => SplitLines(v))
                .Metadata.SetValueComparer(listComparer);
            entity.HasIndex(j => j.CreatedAt);
        });
    }

    private static List<string> SplitLines(string value) =>
        value.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
}
