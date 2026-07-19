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
        sb.AppendLine(Truncate(job.Description, 1000));
        sb.AppendLine();
        sb.AppendLine("Return this exact JSON structure:");
        sb.AppendLine("""
        {
          "score": 8.5,
          "strengths": ["C# experience", "Azure knowledge"],
          "gaps": ["Kubernetes"],
          "hardBlockers": [],
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

        var recommendation = root.GetProperty("recommendation").GetString() switch
        {
            "ApplyNow"          => Recommendation.ApplyNow,
            "ApplyIfInterested" => Recommendation.ApplyIfInterested,
            _                   => Recommendation.Skip
        };

        return new FitAnalysis
        {
            JobId                  = jobId,
            Score                  = root.GetProperty("score").GetDouble(),
            Strengths              = root.GetProperty("strengths").GetRawText(),
            Gaps                   = root.GetProperty("gaps").GetRawText(),
            HardBlockers           = root.GetProperty("hardBlockers").GetRawText(),
            Explanation            = root.GetProperty("explanation").GetString() ?? string.Empty,
            Recommendation         = recommendation,
            ResumeAdvice           = root.GetProperty("resumeAdvice").GetString() ?? string.Empty,
            EstimatedEffortMinutes = root.GetProperty("estimatedEffortMinutes").GetInt32()
        };
    }
}
