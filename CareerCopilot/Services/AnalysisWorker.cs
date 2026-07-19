using CareerCopilot.Data;
using Microsoft.EntityFrameworkCore;

namespace CareerCopilot.Services;

public class AnalysisWorker : BackgroundService
{
    private readonly AnalysisQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AnalysisWorker> _logger;

    public AnalysisWorker(AnalysisQueue queue, IServiceScopeFactory scopeFactory, ILogger<AnalysisWorker> logger)
    {
        _queue        = queue;
        _scopeFactory = scopeFactory;
        _logger       = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Process max 2 jobs concurrently
        var semaphore = new SemaphoreSlim(2);

        await foreach (var jobId in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await semaphore.WaitAsync(stoppingToken);

            _ = Task.Run(async () =>
            {
                try   { await AnalyseJobAsync(jobId, stoppingToken); }
                catch (Exception ex) { _logger.LogError(ex, "Failed to analyse job {JobId}", jobId); }
                finally { semaphore.Release(); }
            }, stoppingToken);
        }
    }

    private async Task AnalyseJobAsync(int jobId, CancellationToken ct)
    {
        using var scope    = _scopeFactory.CreateScope();
        var db             = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var analyzer       = scope.ServiceProvider.GetRequiredService<IFitAnalyzer>();

        var job     = await db.Jobs.FindAsync([jobId], ct);
        var profile = await db.UserProfiles.FirstOrDefaultAsync(ct);

        if (job is null || profile is null) return;

        // Skip if already analysed
        bool exists = await db.FitAnalyses.AnyAsync(f => f.JobId == jobId, ct);
        if (exists) return;

        _logger.LogInformation("Analysing job {JobId}: {Title}", jobId, job.Title);
        var analysis = await analyzer.AnalyzeAsync(job, profile, ct);
        db.FitAnalyses.Add(analysis);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Done: {Title} → {Score}", job.Title, analysis.Score);
    }
}
