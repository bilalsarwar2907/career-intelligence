using CareerCopilot.Models;
using CareerCopilot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace CareerCopilot.Pages;

public record TrackerStats(int Applied, int Interviews, int Offers, int Rejected);

public class TrackerModel : PageModel
{
    private readonly IApplicationTracker _tracker;

    public TrackerModel(IApplicationTracker tracker) => _tracker = tracker;

    public List<ApplicationRecord> Records { get; private set; } = [];
    public TrackerStats Stats { get; private set; } = new(0, 0, 0, 0);

    public async Task OnGetAsync()
    {
        Records = (await _tracker.GetAllAsync()).ToList();
        Stats = new TrackerStats(
            Applied:    Records.Count(r => r.AppliedAt is not null),
            Interviews: Records.Count(r => r.GotInterview == true),
            Offers:     Records.Count(r => r.GotOffer == true),
            Rejected:   Records.Count(r => r.Status == ApplicationStatus.Rejected)
        );
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int jobId, ApplicationStatus status)
    {
        await _tracker.UpsertAsync(jobId, status);
        return RedirectToPage();
    }
}
