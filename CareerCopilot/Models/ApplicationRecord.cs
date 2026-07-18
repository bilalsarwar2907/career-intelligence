namespace CareerCopilot.Models;

public class ApplicationRecord
{
    public int Id { get; set; }
    public int JobId { get; set; }

    public ApplicationStatus Status { get; set; } = ApplicationStatus.Interested;
    public DateTime? AppliedAt { get; set; }
    public DateTime? InterviewAt { get; set; }
    public DateTime? DecisionAt { get; set; }
    public string Notes { get; set; } = string.Empty;

    // Outcome tracking — this is what feeds the feedback loop
    public bool? GotInterview { get; set; }
    public bool? GotOffer { get; set; }
    public string RejectionReason { get; set; } = string.Empty;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public Job Job { get; set; } = null!;
}

public enum ApplicationStatus
{
    Interested,
    Applied,
    PhoneScreen,
    Interview,
    Offer,
    Rejected,
    Withdrawn,
    Skipped
}
