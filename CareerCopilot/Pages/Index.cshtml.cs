using CareerCopilot.Data;
using CareerCopilot.Models;
using CareerCopilot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Pages;

public record DashboardItem(Job Job, FitAnalysis? Analysis);

public class IndexModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IApplicationTracker _tracker;

    public IndexModel(AppDbContext db, IApplicationTracker tracker)
    {
        _db = db;
        _tracker = tracker;
    }

    public List<DashboardItem> Jobs { get; private set; } = [];

    public async Task OnGetAsync()
    {
        var jobs = await _db.Jobs
            .Where(j => j.IsActive)
            .Include(j => j.FitAnalysis)
            .Include(j => j.ApplicationRecord)
            .OrderByDescending(j => j.FitAnalysis != null ? j.FitAnalysis.Score : 0)
            .ToListAsync();

        // Exclude skipped and already applied
        Jobs = jobs
            .Where(j => j.ApplicationRecord?.Status is null or ApplicationStatus.Interested)
            .Where(j => j.FitAnalysis?.Recommendation != Recommendation.Skip)
            .Select(j => new DashboardItem(j, j.FitAnalysis))
            .ToList();
    }

    public async Task<IActionResult> OnPostMarkAppliedAsync(int jobId)
    {
        await _tracker.UpsertAsync(jobId, ApplicationStatus.Applied);
        return RedirectToPage();
    }
}
