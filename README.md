# Career Copilot

A personal Career Intelligence Copilot. Helps you decide which jobs are worth pursuing, explains why, and tracks outcomes.

## Quick Start

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- An OpenAI API key (for fit analysis)

### 1. Set your OpenAI key
Edit `CareerCopilot/appsettings.json`:
```json
"OpenAI": {
  "ApiKey": "sk-...",
  "Model": "gpt-4o-mini"
}
```

### 2. Create & apply the database migration
```bash
cd CareerCopilot
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### 3. Run
```bash
dotnet run
```

Open http://localhost:5000 in your browser.

---

## First Use

1. **My Profile** — add your resume text, skills, and career goals. This is what the AI uses to score jobs.
2. **Jobs → Collect & Analyse** — pulls jobs from the collector and runs AI fit analysis on each.
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
│   ├── JobCollectorStub.cs      # Stub: returns sample jobs (replace this)
│   ├── IFitAnalyzer.cs          # Interface: score a job
│   ├── FitAnalyzerOpenAI.cs     # Real: calls OpenAI structured output
│   ├── IResumeOptimizer.cs      # Interface: resume advice
│   ├── ResumeOptimizerStub.cs   # Stub: basic advice (enhance later)
│   ├── IApplicationTracker.cs   # Interface: CRM operations
│   └── ApplicationTrackerService.cs # Real: EF Core implementation
├── Pages/
│   ├── Index (Dashboard)        # Today's ranked opportunities
│   ├── Jobs                     # All jobs + collect trigger
│   ├── Tracker                  # Application CRM + stats
│   └── Profile (TODO)           # Set up your career profile
└── Program.cs                   # DI wiring + startup
```

---

## What to Build Next

| Priority | Feature | Notes |
|----------|---------|-------|
| High | Profile page | Allow editing UserProfile in the UI |
| High | Real job collector | Replace `JobCollectorStub` with JobSpy or LinkedIn API |
| Medium | Manual job import | Paste a job URL or description directly |
| Medium | Resume optimizer page | Show per-job advice from `IResumeOptimizer` |
| Low | Outcome analytics | Chart interview rate over time |

---

## Success Metrics

Track these, not "applications sent":
- **Interview rate** = Interviews / Applications
- **Hours saved** per week vs. your baseline
- **Offer rate** = Offers / Interviews
