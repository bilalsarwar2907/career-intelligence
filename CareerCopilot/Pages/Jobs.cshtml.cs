using CareerCopilot.Data;
using CareerCopilot.Models;
using CareerCopilot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Pages;

public class JobsModel : PageModel
{
    private readonly AppDbContext  _db;
    private readonly IJobCollector _collector;
    private readonly AnalysisQueue _queue;

    public JobsModel(AppDbContext db, IJobCollector collector, AnalysisQueue queue)
    {
        _db        = db;
        _collector = collector;
        _queue     = queue;
    }

    public List<Job> Jobs          { get; private set; } = [];
    public string?   StatusMessage { get; private set; }

    public async Task OnGetAsync()
    {
        Jobs = await _db.Jobs
            .Include(j => j.FitAnalysis)
            .OrderByDescending(j => j.FitAnalysis != null ? j.FitAnalysis.Score : -1)
            .ToListAsync();
    }

    // ── Collect from Python collector ─────────────────────────────────────────
    public async Task<IActionResult> OnPostCollectAsync()
    {
        var profile = await _db.UserProfiles.FirstOrDefaultAsync();
        if (profile is null)
        {
            StatusMessage = "Please set up your profile first.";
            await OnGetAsync();
            return Page();
        }

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

    // ── Manual import ─────────────────────────────────────────────────────────
    public async Task<IActionResult> OnPostImportAsync(
        string title, string company, string? location,
        string? url,  string description)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(company) || string.IsNullOrWhiteSpace(description))
        {
            StatusMessage = "Title, Company, and Description are required.";
            await OnGetAsync();
            return Page();
        }

        // Deduplicate — same title + company already in DB
        bool exists = await _db.Jobs.AnyAsync(j => j.Title == title && j.Company == company);
        if (exists)
        {
            StatusMessage = $"'{title}' at {company} is already in your job list.";
            await OnGetAsync();
            return Page();
        }

        var job = new Job
        {
            Title       = title.Trim(),
            Company     = company.Trim(),
            Location    = location?.Trim() ?? string.Empty,
            Url         = url?.Trim()      ?? string.Empty,
            Description = description.Trim(),
            Source      = "Manual",
            DateFound   = DateTime.UtcNow,
        };

        _db.Jobs.Add(job);
        await _db.SaveChangesAsync();

        _queue.Enqueue(job.Id);

        StatusMessage = $"Imported '{job.Title}' at {job.Company} — queued for analysis. Refresh in a minute to see the score.";
        await OnGetAsync();
        return Page();
    }
}
