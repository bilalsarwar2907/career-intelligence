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
    private readonly IFitAnalyzer _analyzer;

    public JobsModel(AppDbContext db, IJobCollector collector, IFitAnalyzer analyzer)
    {
        _db = db;
        _collector = collector;
        _analyzer = analyzer;
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

        // 3. Analyse ALL jobs that don't have analysis yet — in parallel
        var unanalysed = await _db.Jobs
            .Where(j => j.FitAnalysis == null)
            .ToListAsync();

        var analyses = await Task.WhenAll(
            unanalysed.Select(job => _analyzer.AnalyzeAsync(job, profile))
        );

        foreach (var analysis in analyses)
        {
            _db.FitAnalyses.Add(analysis);
        }
        await _db.SaveChangesAsync();

        StatusMessage = $"Added {added} new job(s). Analysed {unanalysed.Count} job(s).";
        await OnGetAsync();
        return Page();
    }
}
