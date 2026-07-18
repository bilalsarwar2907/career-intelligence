using CareerCopilot.Models;

namespace CareerCopilot.Services;

/// <summary>
/// Fetches jobs from external sources (JobSpy, LinkedIn, Indeed, manual import).
/// Returns raw Job objects — no AI involved at this stage.
/// </summary>
public interface IJobCollector
{
    Task<IEnumerable<Job>> CollectAsync(CancellationToken ct = default);
}
