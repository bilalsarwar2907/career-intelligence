using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CareerCopilot.Models;

namespace CareerCopilot.Services;

public class ResumeOptimizerOllama : IResumeOptimizer
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly ILogger<ResumeOptimizerOllama> _logger;

    public ResumeOptimizerOllama(IConfiguration config, ILogger<ResumeOptimizerOllama> logger)
    {
        _logger = logger;
        var baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _model      = config["Ollama:Model"]   ?? "phi3:mini";
        _http       = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(300) };
    }

    public async Task<ResumeAdvice> GetAdviceAsync(Job job, UserProfile profile, CancellationToken ct = default)
    {
        var prompt  = BuildPrompt(job, profile);
        var payload = new { model = _model, prompt, stream = false, format = "json" };

        _logger.LogInformation("Requesting resume advice from Ollama: {Title} at {Company}", job.Title, job.Company);

        var response = await _http.PostAsJsonAsync("/api/generate", payload, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        using var doc    = JsonDocument.Parse(body);
        var responseText = doc.RootElement.GetProperty("response").GetString() ?? "{}";

        return ParseResponse(responseText);
    }

    // ── Helpers ──────────────────────────────────────────────────────────────────

    private static string Truncate(string? text, int max) =>
        string.IsNullOrEmpty(text) ? "" :
        text.Length <= max ? text : text[..max] + "…";

    private static string BuildPrompt(Job job, UserProfile profile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a professional resume coach.");
        sb.AppendLine("Give specific, actionable advice on how to tailor this candidate's resume for this specific job.");
        sb.AppendLine();
        sb.AppendLine("Rules:");
        sb.AppendLine("- NEVER invent experience the candidate does not have.");
        sb.AppendLine("- Only reorder, reframe, and highlight what already exists in their profile.");
        sb.AppendLine("- Be specific — name actual skills from the candidate's profile and actual requirements from the job.");
        sb.AppendLine("- highlightBullets must be based on the candidate's real experience — rewrite them to lead with the most relevant angle for this role.");
        sb.AppendLine("Return ONLY valid JSON — no explanation, no markdown, no code block. Just the JSON object.");
        sb.AppendLine();
        sb.AppendLine("## Candidate");
        sb.AppendLine($"Name: {profile.Name}");
        sb.AppendLine($"Skills: {profile.Skills}");
        sb.AppendLine($"Preferred Technologies: {profile.PreferredTechnologies}");
        sb.AppendLine($"Career Goals: {Truncate(profile.CareerGoals, 300)}");
        sb.AppendLine();
        sb.AppendLine("## Resume");
        sb.AppendLine(Truncate(profile.ResumeText, 2000));
        sb.AppendLine();
        sb.AppendLine("## Target Job");
        sb.AppendLine($"{job.Title} at {job.Company} ({job.Location})");
        sb.AppendLine(Truncate(job.Description, 1200));
        sb.AppendLine();
        sb.AppendLine("Return this exact JSON structure:");
        sb.AppendLine("""
        {
          "summary": "One short paragraph: what to change and why, specific to this job.",
          "highlightSkills": ["MostRelevantSkill", "SecondSkill", "ThirdSkill"],
          "highlightBullets": [
            "Led migration of X to Y, reducing latency by Z% — relevant because job requires distributed systems",
            "Built REST API serving N requests/day — relevant because job is API-focused"
          ]
        }
        """);
        sb.AppendLine("highlightSkills: 3–5 skills from the candidate's profile that most match this job, most relevant first.");
        sb.AppendLine("highlightBullets: 2–4 experience bullets the candidate should move to the top of their resume for this role.");
        return sb.ToString();
    }

    private static ResumeAdvice ParseResponse(string json)
    {
        // Strip markdown fences if model added them
        json = json.Trim();
        if (json.StartsWith("```")) json = json.Split('\n', 2)[1];
        if (json.EndsWith("```"))  json = json[..^3];

        using var doc = JsonDocument.Parse(json.Trim());
        var root = doc.RootElement;

        static string GetString(JsonElement el, string key) =>
            el.TryGetProperty(key, out var p) ? p.GetString() ?? string.Empty : string.Empty;

        static List<string> GetArray(JsonElement el, string key)
        {
            if (!el.TryGetProperty(key, out var p)) return [];
            return p.EnumerateArray()
                    .Select(x => x.GetString() ?? string.Empty)
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .ToList();
        }

        return new ResumeAdvice(
            Summary:          GetString(root, "summary"),
            HighlightSkills:  GetArray(root,  "highlightSkills"),
            HighlightBullets: GetArray(root,  "highlightBullets")
        );
    }
}
