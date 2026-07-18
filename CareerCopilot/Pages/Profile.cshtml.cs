using CareerCopilot.Data;
using CareerCopilot.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Pages;

public class ProfileModel : PageModel
{
    private readonly AppDbContext _db;

    public ProfileModel(AppDbContext db) => _db = db;

    [BindProperty]
    public string Name { get; set; } = string.Empty;
    [BindProperty]
    public string ResumeText { get; set; } = string.Empty;
    [BindProperty]
    public string Skills { get; set; } = string.Empty;
    [BindProperty]
    public string PreferredTitles { get; set; } = string.Empty;
    [BindProperty]
    public string PreferredLocations { get; set; } = string.Empty;
    [BindProperty]
    public string PreferredTechnologies { get; set; } = string.Empty;
    [BindProperty]
    public int? MinSalary { get; set; }
    [BindProperty]
    public string CareerGoals { get; set; } = string.Empty;

    public bool Saved { get; private set; }

    public async Task OnGetAsync()
    {
        var p = await _db.UserProfiles.FirstOrDefaultAsync();
        if (p is null) return;

        Name                  = p.Name;
        ResumeText            = p.ResumeText;
        Skills                = p.Skills;
        PreferredTitles       = p.PreferredTitles;
        PreferredLocations    = p.PreferredLocations;
        PreferredTechnologies = p.PreferredTechnologies;
        MinSalary             = p.MinSalary;
        CareerGoals           = p.CareerGoals;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var existing = await _db.UserProfiles.FirstOrDefaultAsync();

        if (existing is null)
        {
            _db.UserProfiles.Add(new UserProfile
            {
                Name                  = Name,
                ResumeText            = ResumeText,
                Skills                = Skills,
                PreferredTitles       = PreferredTitles,
                PreferredLocations    = PreferredLocations,
                PreferredTechnologies = PreferredTechnologies,
                MinSalary             = MinSalary,
                CareerGoals           = CareerGoals,
                UpdatedAt             = DateTime.UtcNow
            });
        }
        else
        {
            existing.Name                  = Name;
            existing.ResumeText            = ResumeText;
            existing.Skills                = Skills;
            existing.PreferredTitles       = PreferredTitles;
            existing.PreferredLocations    = PreferredLocations;
            existing.PreferredTechnologies = PreferredTechnologies;
            existing.MinSalary             = MinSalary;
            existing.CareerGoals           = CareerGoals;
            existing.UpdatedAt             = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        Saved = true;
        return Page();
    }
}
