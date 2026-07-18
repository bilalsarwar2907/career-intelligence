namespace CareerCopilot.Models;

public class FitAnalysis
{
    public int Id { get; set; }
    public int JobId { get; set; }

    public double Score { get; set; }                    // 0.0 – 10.0
    public string Strengths { get; set; } = string.Empty;   // JSON array
    public string Gaps { get; set; } = string.Empty;        // JSON array
    public string HardBlockers { get; set; } = string.Empty;// JSON array
    public string Explanation { get; set; } = string.Empty; // Free-text paragraph
    public Recommendation Recommendation { get; set; }
    public string ResumeAdvice { get; set; } = string.Empty;// What to reorder/highlight
    public int EstimatedEffortMinutes { get; set; }

    public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Job Job { get; set; } = null!;
}

public enum Recommendation
{
    ApplyNow,
    ApplyIfInterested,
    Skip
}
