using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using CareerCopilot.Models;

namespace CareerCopilot.Services;

public class FitAnalyzerOllama : IFitAnalyzer
{
    private readonly HttpClient _http;
    private readonly string _model;
    private readonly ILogger<FitAnalyzerOllama> _logger;

    public FitAnalyzerOllama(IConfiguration config, ILogger<FitAnalyzerOllama> logger)
    {
        _logger = logger;
        var baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
        _model     = config["Ollama:Model"]   ?? "llama3";
        _http = new HttpClient { BaseAddress = new Uri(baseUrl), Timeout = TimeSpan.FromSeconds(300) };
    }

    public async Task<FitAnalysis> AnalyzeAsync(Job job, UserProfile profile, CancellationToken ct = default)
    {
        var prompt = BuildPrompt(job, profile);

        var payload = new
        {
            model  = _model,
            prompt = prompt,
            stream = false,
            format = "json"
        };

        var response = await _http.PostAsJsonAsync("/api/generate", payload, ct);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(ct);
        _logger.LogDebug("Ollama raw response: {Body}", body);

        using var doc  = JsonDocument.Parse(body);
        var responseText = doc.RootElement.GetProperty("response").GetString() ?? "{}";

        return ParseResponse(responseText, job.Id);
    }

    private static string Truncate(string? text, int max) =>
        string.IsNullOrEmpty(text) ? "" :
        text.Length <= max ? text : text[..max] + "…";

    private static string BuildPrompt(Job job, UserProfile profile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a career advisor. Analyze how well this candidate fits this job.");
        sb.AppendLine("Return ONLY valid JSON — no explanation, no markdown, no code block. Just the JSON object.");
        sb.AppendLine();
        sb.AppendLine("## Candidate");
        sb.AppendLine($"Skills: {profile.Skills}");
        sb.AppendLine($"Career Goals: {Truncate(profile.CareerGoals, 300)}");
        sb.AppendLine($"Preferred Technologies: {profile.PreferredTechnologies}");
        sb.AppendLine();
        sb.AppendLine("## Resume Summary");
        sb.AppendLine(Truncate(profile.ResumeText, 1500));
        sb.AppendLine();
        sb.AppendLine("## Job");
        sb.AppendLine($"{job.Title} at {job.Company} ({job.Location})");
        sb.AppendLine($"Found: {job.DateFound:yyyy-MM-dd} ({(int)(DateTime.UtcNow - job.DateFound).TotalDays} days ago)");
        sb.AppendLine(Truncate(job.Description, 1000));
        sb.AppendLine();
        sb.AppendLine("## Anomaly Check");
        sb.AppendLine("Before scoring, check for any of these red flags and include them in the anomalies array:");
        sb.AppendLine("- Job was found more than 30 days ago (likely filled or budget frozen)");
        sb.AppendLine("- Tech stack listed conflicts with the seniority level (e.g. 'junior' requiring 5+ years)");
        sb.AppendLine("- No salary or compensation mentioned");
        sb.AppendLine("- Location or remote policy conflicts with candidate's preferred locations");
        sb.AppendLine("- Job description is a generic template (no specific product, team, or project mentioned)");
        sb.AppendLine("- Required experience is unrealistic for the stated level");
        sb.AppendLine("If no anomalies, return an empty array.");
        sb.AppendLine();
        sb.AppendLine("## Skill Flagging Rules");
        sb.AppendLine("- If a required skill is COMPLETELY ABSENT from the candidate's profile (not mentioned anywhere), add it to hardBlockers with prefix \"UNKNOWN:\" — do NOT infer capability or move it to gaps.");
        sb.AppendLine("- Only use gaps for skills the candidate has partial or adjacent experience with.");
        sb.AppendLine("- Example: job requires Kubernetes, candidate never mentioned containers → hardBlockers: [\"UNKNOWN: Kubernetes\"]");
        sb.AppendLine();
        sb.AppendLine("Return this exact JSON structure:");
        sb.AppendLine("""
        {
          "score": 8.5,
          "strengths": ["C# experience", "Azure knowledge"],
          "gaps": ["Kubernetes"],
          "hardBlockers": [],
          "anomalies": ["Job found 45 days ago — may already be filled"],
          "explanation": "One paragraph explaining the fit.",
          "recommendation": "ApplyNow",
          "resumeAdvice": "Move Azure above AWS. Highlight REST API project.",
          "estimatedEffortMinutes": 15
        }
        """);
        sb.AppendLine("recommendation must be one of: ApplyNow, ApplyIfInterested, Skip");
        return sb.ToString();
    }

    private static FitAnalysis ParseResponse(string json, int jobId)
    {
        // Strip markdown code fences if the model added them anyway
        json = json.Trim();
        if (json.StartsWith("```")) json = json.Split('\n', 2)[1];
        if (json.EndsWith("```")) json  = json[..^3];

        using var doc = JsonDocument.Parse(json.Trim());
        var root = doc.RootElement;

        static string GetArray(JsonElement el, string key) =>
            el.TryGetProperty(key, out var p) ? p.GetRawText() : "[]";

        static string GetString(JsonElement el, string key) =>
            el.TryGetProperty(key, out var p) ? p.GetString() ?? string.Empty : string.Empty;

        static double GetDouble(JsonElement el, string key) =>
            el.TryGetProperty(key, out var p) ? p.GetDouble() : 0.0;

        static int GetInt(JsonElement el, string key) =>
            el.TryGetProperty(key, out var p) ? p.GetInt32() : 0;

        var recommendation = GetString(root, "recommendation") switch
        {
            "ApplyNow"          => Recommendation.ApplyNow,
            "ApplyIfInterested" => Recommendation.ApplyIfInterested,
            _                   => Recommendation.Skip
        };

        return new FitAnalysis
        {
            JobId                  = jobId,
            Score                  = GetDouble(root, "score"),
            Strengths              = GetArray(root, "strengths"),
            Gaps                   = GetArray(root, "gaps"),
            HardBlockers           = GetArray(root, "hardBlockers"),
            Anomalies              = GetArray(root, "anomalies"),
            Explanation            = GetString(root, "explanation"),
            Recommendation         = recommendation,
            ResumeAdvice           = GetString(root, "resumeAdvice"),
            EstimatedEffortMinutes = GetInt(root, "estimatedEffortMinutes")
        };
    }
}
