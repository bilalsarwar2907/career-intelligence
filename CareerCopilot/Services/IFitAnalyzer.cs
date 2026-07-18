using CareerCopilot.Models;

namespace CareerCopilot.Services;

/// <summary>
/// Core AI service. Given a job and the user's profile, produces a FitAnalysis
/// with score, explanation, gaps, hard blockers, and Apply/Skip recommendation.
/// </summary>
public interface IFitAnalyzer
{
    Task<FitAnalysis> AnalyzeAsync(Job job, UserProfile profile, CancellationToken ct = default);
}
