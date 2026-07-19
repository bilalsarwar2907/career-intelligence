using System.Text.Json;
using CareerCopilot.Models;

namespace CareerCopilot.Services;

/// <summary>
/// Reads jobs from jobs.json produced by JobCollector/collect_jobs.py.
/// Run the Python script first, then hit "Collect & Analyse" in the UI.
/// </summary>
public class JobCollectorJson : IJobCollector
{
    private readonly string _jsonPath;
    private readonly ILogger<JobCollectorJson> _logger;

    public JobCollectorJson(IConfiguration config, ILogger<JobCollectorJson> logger)
    {
        _logger = logger;
        _jsonPath = config["JobCollector:JsonPath"]
                    ?? Path.Combine(AppContext.BaseDirectory, "jobs.json");
    }

    public Task<IEnumerable<Job>> CollectAsync(CancellationToken ct = default)
    {
        if (!File.Exists(_jsonPath))
        {
            _logger.LogWarning("jobs.json not found at {Path}. Run collect_jobs.py first.", _jsonPath);
            return Task.FromResult<IEnumerable<Job>>([]);
        }

        var json    = File.ReadAllText(_jsonPath);
        using var doc = JsonDocument.Parse(json);
        var jobsArr = doc.RootElement.GetProperty("jobs");

        var jobs = new List<Job>();
        foreach (var item in jobsArr.EnumerateArray())
        {
            var title = item.GetProperty("title").GetString() ?? string.Empty;
            var company = item.GetProperty("company").GetString() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(company))
                continue;

            jobs.Add(new Job
            {
                Title       = title,
                Company     = company,
                Location    = item.GetProperty("location").GetString() ?? string.Empty,
                Description = item.GetProperty("description").GetString() ?? string.Empty,
                Url         = item.GetProperty("url").GetString() ?? string.Empty,
                Source      = item.GetProperty("source").GetString() ?? "JobSpy",
            });
        }

        _logger.LogInformation("Loaded {Count} jobs from {Path}", jobs.Count, _jsonPath);
        return Task.FromResult<IEnumerable<Job>>(jobs);
    }
}
