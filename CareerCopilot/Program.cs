using CareerCopilot.Data;
using CareerCopilot.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ── Database ──────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
                      ?? "Data Source=career_copilot.db"));

// ── Services ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IJobCollector, JobCollectorJson>();       // Reads jobs.json from Python collector
builder.Services.AddScoped<IFitAnalyzer, FitAnalyzerOllama>();      // Local Ollama (llama3)
builder.Services.AddScoped<IResumeOptimizer, ResumeOptimizerStub>(); // TODO: wire to LLM
builder.Services.AddScoped<IApplicationTracker, ApplicationTrackerService>();

// ── Razor Pages ───────────────────────────────────────────────────────────────
builder.Services.AddSingleton<AnalysisQueue>();
builder.Services.AddHostedService<AnalysisWorker>();
builder.Services.AddRazorPages();

var app = builder.Build();

// ── Auto-migrate on startup ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
