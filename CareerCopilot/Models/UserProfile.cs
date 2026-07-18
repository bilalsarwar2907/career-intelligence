namespace CareerCopilot.Models;

public class UserProfile
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? ResumeText { get; set; }    // Full resume as plain text
    public string? Skills { get; set; }        // Comma-separated
    public string? PreferredTitles { get; set; }
    public string? PreferredLocations { get; set; }
    public string? PreferredTechnologies { get; set; }
    public int? MinSalary { get; set; }
    public string? CareerGoals { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
