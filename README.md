# Career Copilot

A personal Career Intelligence Copilot. Helps you decide which jobs are worth pursuing, explains why, and tracks outcomes.

## Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Ollama](https://ollama.ai) running locally with `phi3:mini` pulled (default LLM — no API key needed)
- Python 3.x + `pip install python-jobspy requests beautifulsoup4 lxml` (for job collection)
- Optional: OpenAI API key if you prefer GPT-4o-mini over Ollama

### 1. Create & apply the database migration
```bash
cd CareerCopilot
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 2. Collect fresh jobs
```bash
cd JobCollector
python collect_jobs.py
```
This writes `CareerCopilot/jobs.json`. Re-run daily to get new listings.

### 3. Run the app
```bash
cd CareerCopilot
dotnet run
```

Open http://localhost:5000 in your browser.

---

## First Use

1. **My Profile** — add your resume text, skills, and career goals. This is what the AI uses to score jobs.
2. **Jobs → Collect & Analyse** — imports jobs from `jobs.json` and runs AI fit analysis on each.
3. **Dashboard** — your ranked recommendations for today. Apply or skip.
4. **Tracker** — update status as applications progress. This is how you close the feedback loop.

---

## Project Structure

```
CareerCopilot/
├── Data/
│   └── AppDbContext.cs          # EF Core DbContext (SQLite)
├── Models/
│   ├── Job.cs                   # Raw job data
│   ├── UserProfile.cs           # Your resume + preferences
│   ├── FitAnalysis.cs           # AI scoring output
│   └── ApplicationRecord.cs    # CRM — tracks outcomes
├── Services/
│   ├── IJobCollector.cs         # Interface: fetch jobs
│   ├── JobCollectorJson.cs      # Real: reads jobs.json from collect_jobs.py
│   ├── IFitAnalyzer.cs          # Interface: score a job
│   ├── FitAnalyzerOllama.cs     # Real: calls local Ollama (default)
│   ├── FitAnalyzerOpenAI.cs     # Real: calls OpenAI (optional)
│   ├── IResumeOptimizer.cs      # Interface: resume advice
│   ├── ResumeOptimizerStub.cs   # Stub: basic advice (enhance later)
│   ├── IApplicationTracker.cs   # Interface: CRM operations
│   └── ApplicationTrackerService.cs # Real: EF Core implementation
├── Pages/
│   ├── Index (Dashboard)        # Today's ranked opportunities
│   ├── Jobs                     # All jobs + collect trigger
│   ├── JobDetail                # Score, strengths, gaps, blockers, resume advice
│   ├── Tracker                  # Application CRM + stats
│   └── Profile                  # Set up your career profile
└── Program.cs                   # DI wiring + startup
```

---

## What to Build Next

| Priority | Feature | Notes |
|----------|---------|-------|
| Medium | Manual job import | Paste a job URL or description directly |
| Medium | Resume optimizer page | Show per-job advice from `IResumeOptimizer` |
| Low | Outcome analytics | Chart interview rate over time |
| Low | Learning Loop (Layer 10) | Feed outcomes back into AI scoring (build after 4–8 weeks of data) |

---

## Switching to OpenAI

By default the app uses Ollama (`phi3:mini`). To use OpenAI instead:

```bash
# Set your key (never edit appsettings.json directly)
cd CareerCopilot
dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
```

Then in `Program.cs` change:
```csharp
services.AddScoped<IFitAnalyzer, FitAnalyzerOpenAI>();
```

---

## Success Metrics

Track these, not "applications sent":
- **Interview rate** = Interviews / Applications
- **Hours saved** per week vs. your baseline
- **Offer rate** = Offers / Interviews
