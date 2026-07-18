namespace CareerCopilot.Models;

public class Job
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // LinkedIn, Indeed, Manual, etc.
    public DateTime DateFound { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;

    // Navigation
    public FitAnalysis? FitAnalysis { get; set; }
    public ApplicationRecord? ApplicationRecord { get; set; }
}
