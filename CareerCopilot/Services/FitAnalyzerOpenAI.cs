using System.Text;
using System.Text.Json;
using CareerCopilot.Models;
using OpenAI.Chat;

namespace CareerCopilot.Services;

/// <summary>
/// Real implementation using OpenAI Chat Completions.
/// Prompts the LLM to return structured JSON matching FitAnalysisDto,
/// then maps it to a FitAnalysis entity.
/// </summary>
public class FitAnalyzerOpenAI : IFitAnalyzer
{
    private readonly ChatClient _client;
    private readonly ILogger<FitAnalyzerOpenAI> _logger;

    public FitAnalyzerOpenAI(IConfiguration config, ILogger<FitAnalyzerOpenAI> logger)
    {
        _logger = logger;
        var apiKey = config["OpenAI:ApiKey"] ?? throw new InvalidOperationException("OpenAI:ApiKey not set");
        var model  = config["OpenAI:Model"] ?? "gpt-4o-mini";
        _client = new ChatClient(model, apiKey);
    }

    public async Task<FitAnalysis> AnalyzeAsync(Job job, UserProfile profile, CancellationToken ct = default)
    {
        var prompt = BuildPrompt(job, profile);

        var response = await _client.CompleteChatAsync(
            [new UserChatMessage(prompt)],
            new ChatCompletionOptions { ResponseFormat = ChatResponseFormat.CreateJsonObjectFormat() },
            ct);

        var json = response.Value.Content[0].Text;
        _logger.LogDebug("FitAnalyzer raw response: {Json}", json);

        return ParseResponse(json, job.Id);
    }

    private static string BuildPrompt(Job job, UserProfile profile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are a career advisor. Analyze how well this candidate fits this job.");
        sb.AppendLine();
        sb.AppendLine("## Candidate Profile");
        sb.AppendLine($"Skills: {profile.Skills}");
        sb.AppendLine($"Career Goals: {profile.CareerGoals}");
        sb.AppendLine($"Preferred Technologies: {profile.PreferredTechnologies}");
        sb.AppendLine();
        sb.AppendLine("## Resume");
        sb.AppendLine(profile.ResumeText);
        sb.AppendLine();
        sb.AppendLine("## Job Description");
        sb.AppendLine($"Title: {job.Title} at {job.Company} ({job.Location})");
        sb.AppendLine($"Found: {job.DateFound:yyyy-MM-dd} ({(int)(DateTime.UtcNow - job.DateFound).TotalDays} days ago)");
        sb.AppendLine(job.Description);
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
        sb.AppendLine("Return ONLY valid JSON in this exact structure:");
        sb.AppendLine("""
        {
          "score": 8.5,
          "strengths": ["C# experience", "Azure knowledge"],
          "gaps": ["Kubernetes"],
          "hardBlockers": [],
          "anomalies": ["Job found 45 days ago — may already be filled"],
          "explanation": "One paragraph explaining the fit.",
          "recommendation": "ApplyNow",
          "resumeAdvice": "Move Azure above AWS in skills section. Highlight REST API project.",
          "estimatedEffortMinutes": 15
        }
        """);
        sb.AppendLine("recommendation must be one of: ApplyNow, ApplyIfInterested, Skip");
        return sb.ToString();
    }

    private static FitAnalysis ParseResponse(string json, int jobId)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var recommendation = root.GetProperty("recommendation").GetString() switch
        {
            "ApplyNow"           => Recommendation.ApplyNow,
            "ApplyIfInterested"  => Recommendation.ApplyIfInterested,
            _                    => Recommendation.Skip
        };

        return new FitAnalysis
        {
            JobId                  = jobId,
            Score                  = root.GetProperty("score").GetDouble(),
            Strengths              = root.GetProperty("strengths").GetRawText(),
            Gaps                   = root.GetProperty("gaps").GetRawText(),
            HardBlockers           = root.GetProperty("hardBlockers").GetRawText(),
            Anomalies              = root.TryGetProperty("anomalies", out var anomProp) ? anomProp.GetRawText() : "[]",
            Explanation            = root.GetProperty("explanation").GetString() ?? string.Empty,
            Recommendation         = recommendation,
            ResumeAdvice           = root.GetProperty("resumeAdvice").GetString() ?? string.Empty,
            EstimatedEffortMinutes = root.GetProperty("estimatedEffortMinutes").GetInt32()
        };
    }
}
