using CareerCopilot.Data;
using CareerCopilot.Models;
using CareerCopilot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Pages;

public class JobsModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly IJobCollector _collector;
    private readonly AnalysisQueue _queue;

    public JobsModel(AppDbContext db, IJobCollector collector, AnalysisQueue queue)
    {
        _db        = db;
        _collector = collector;
        _queue     = queue;
    }

    public List<Job> Jobs { get; private set; } = [];
    public string? StatusMessage { get; private set; }

    public async Task OnGetAsync()
    {
        Jobs = await _db.Jobs
            .Include(j => j.FitAnalysis)
            .OrderByDescending(j => j.FitAnalysis != null ? j.FitAnalysis.Score : -1)
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostCollectAsync()
    {
        // 1. Get the user profile (single-user: always row 1)
        var profile = await _db.UserProfiles.FirstOrDefaultAsync();
        if (profile is null)
        {
            StatusMessage = "Please set up your profile first.";
            await OnGetAsync();
            return Page();
        }

        // 2. Collect and save new jobs
        var incoming = await _collector.CollectAsync();
        int added = 0;

        foreach (var job in incoming)
        {
            bool exists = await _db.Jobs.AnyAsync(j => j.Title == job.Title && j.Company == job.Company);
            if (exists) continue;

            _db.Jobs.Add(job);
            await _db.SaveChangesAsync();
            added++;
        }

        // 3. Enqueue unanalysed jobs for background processing
        var unanalysedIds = await _db.Jobs
            .Where(j => j.FitAnalysis == null)
            .Select(j => j.Id)
            .ToListAsync();

        foreach (var id in unanalysedIds)
            _queue.Enqueue(id);

        StatusMessage = $"Added {added} new job(s). Queued {unanalysedIds.Count} job(s) for analysis — refresh in a minute to see scores.";
        await OnGetAsync();
        return Page();
    }
}
