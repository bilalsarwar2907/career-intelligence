using CareerCopilot.Data;
using CareerCopilot.Models;
using CareerCopilot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Pages;

public class JobDetailModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IApplicationTracker _tracker;

    public JobDetailModel(AppDbContext db, IApplicationTracker tracker)
    {
        _db      = db;
        _tracker = tracker;
    }

    public Job Job { get; private set; } = null!;
    public FitAnalysis? Analysis { get; private set; }
    public ApplicationRecord? ApplicationRecord { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var job = await _db.Jobs
            .Include(j => j.FitAnalysis)
            .Include(j => j.ApplicationRecord)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job is null) return NotFound();

        Job               = job;
        Analysis          = job.FitAnalysis;
        ApplicationRecord = job.ApplicationRecord;
        return Page();
    }

    public async Task<IActionResult> OnPostApplyAsync(int id)
    {
        await _tracker.UpsertAsync(id, ApplicationStatus.Applied);
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostUpdateStatusAsync(int id, ApplicationStatus status, string? notes)
    {
        await _tracker.UpsertAsync(id, status, notes);
        return RedirectToPage(new { id });
    }
}
