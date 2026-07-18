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
        sb.AppendLine(job.Description);
        sb.AppendLine();
        sb.AppendLine("Return ONLY valid JSON in this exact structure:");
        sb.AppendLine("""
        {
          "score": 8.5,
          "strengths": ["C# experience", "Azure knowledge"],
          "gaps": ["Kubernetes"],
          "hardBlockers": [],
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
            Explanation            = root.GetProperty("explanation").GetString() ?? string.Empty,
            Recommendation         = recommendation,
            ResumeAdvice           = root.GetProperty("resumeAdvice").GetString() ?? string.Empty,
            EstimatedEffortMinutes = root.GetProperty("estimatedEffortMinutes").GetInt32()
        };
    }
}
