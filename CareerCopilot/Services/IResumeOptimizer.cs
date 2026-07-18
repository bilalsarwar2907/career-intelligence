using CareerCopilot.Models;

namespace CareerCopilot.Services;

public record ResumeAdvice(
    string Summary,              // Short paragraph explaining what to change
    List<string> HighlightSkills,// Skills to move up / emphasise
    List<string> HighlightBullets// Experience bullets to surface for this role
);

/// <summary>
/// Conservative resume advisor. Reorders and highlights — never invents experience.
/// </summary>
public interface IResumeOptimizer
{
    Task<ResumeAdvice> GetAdviceAsync(Job job, UserProfile profile, CancellationToken ct = default);
}
