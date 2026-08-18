using CareerCopilot.Data;
using CareerCopilot.Models;
using CareerCopilot.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Pages;

public class ResumeOptimizerModel : PageModel
{
    private readonly AppDbContext     _db;
    private readonly IResumeOptimizer _optimizer;

    public ResumeOptimizerModel(AppDbContext db, IResumeOptimizer optimizer)
    {
        _db        = db;
        _optimizer = optimizer;
    }

    public Job          Job      { get; private set; } = null!;
    public FitAnalysis? Analysis { get; private set; }
    public ResumeAdvice? Advice  { get; private set; }
    public string?      Error    { get; private set; }

    // ── GET: show the job summary + Generate button ───────────────────────────
    public async Task<IActionResult> OnGetAsync(int id)
    {
        return await LoadJobAsync(id) ?? Page();
    }

    // ── POST: call Ollama and show advice ─────────────────────────────────────
    public async Task<IActionResult> OnPostAsync(int id)
    {
        var notFound = await LoadJobAsync(id);
        if (notFound is not null) return notFound;

        var profile = await _db.UserProfiles.FirstOrDefaultAsync();
        if (profile is null)
        {
            Error = "No profile set up yet. Go to My Profile and fill in your details first.";
            return Page();
        }

        if (string.IsNullOrWhiteSpace(profile.ResumeText))
        {
            Error = "Your profile has no Resume Text. Add it in My Profile so the advisor has something to work with.";
            return Page();
        }

        try
        {
            Advice = await _optimizer.GetAdviceAsync(Job, profile);
        }
        catch (Exception ex)
        {
            Error = $"Could not get advice ({ex.Message}). Make sure Ollama is running: ollama serve";
        }

        return Page();
    }

    // ── Shared ────────────────────────────────────────────────────────────────
    private async Task<IActionResult?> LoadJobAsync(int id)
    {
        var job = await _db.Jobs
            .Include(j => j.FitAnalysis)
            .FirstOrDefaultAsync(j => j.Id == id);

        if (job is null) return NotFound();

        Job      = job;
        Analysis = job.FitAnalysis;
        return null;
    }
}
