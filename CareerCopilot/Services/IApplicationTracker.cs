using CareerCopilot.Models;

namespace CareerCopilot.Services;

public interface IApplicationTracker
{
    Task<ApplicationRecord> UpsertAsync(int jobId, ApplicationStatus status, string? notes = null, CancellationToken ct = default);
    Task<IEnumerable<ApplicationRecord>> GetAllAsync(CancellationToken ct = default);
    Task<ApplicationRecord?> GetByJobAsync(int jobId, CancellationToken ct = default);
}
