using ResumeAPI.DTOs;
using ResumeAPI.Models;
using ResumeAPI.Repositories;

namespace ResumeAPI.Services;

// ──────────────────────────────────────────────
// Interface
// ──────────────────────────────────────────────

public interface IResumeService
{
    Task<ResumeResponseDto> UploadResumeAsync(ResumeUploadDto dto);
    Task<Resume?> GetResumeByIdAsync(Guid id);
}

// ──────────────────────────────────────────────
// Implementation
// ──────────────────────────────────────────────

/// <summary>
/// Handles business logic for resume upload and retrieval.
/// </summary>
public class ResumeService : IResumeService
{
    private readonly IResumeRepository _repo;
    private readonly ILogger<ResumeService> _logger;

    public ResumeService(IResumeRepository repo, ILogger<ResumeService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    /// <summary>
    /// Validates, maps, and stores an uploaded resume.
    /// Returns a response DTO with the generated ID.
    /// </summary>
    public async Task<ResumeResponseDto> UploadResumeAsync(ResumeUploadDto dto)
    {
        // Basic validation
        if (string.IsNullOrWhiteSpace(dto.Content))
            throw new ArgumentException("Resume content cannot be empty.");

        if (string.IsNullOrWhiteSpace(dto.CandidateName))
            throw new ArgumentException("Candidate name cannot be empty.");

        // Map DTO → domain model
        var resume = new Resume
        {
            CandidateName = dto.CandidateName.Trim(),
            Content = dto.Content.Trim()
        };

        // Persist via repository
        var saved = await _repo.AddAsync(resume);
        _logger.LogInformation("Resume uploaded for {Name} with ID {Id}", saved.CandidateName, saved.Id);

        // Map saved entity → response DTO
        return new ResumeResponseDto
        {
            Id = saved.Id,
            CandidateName = saved.CandidateName,
            UploadedAt = saved.UploadedAt,
            Message = "Resume uploaded successfully."
        };
    }

    /// <summary>Retrieves a resume by ID for use in matching.</summary>
    public async Task<Resume?> GetResumeByIdAsync(Guid id)
    {
        return await _repo.GetByIdAsync(id);
    }
}
