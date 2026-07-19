# Career Intelligence Copilot — Session Summary

## Primary Goal
Build a "Career Intelligence Copilot" — a personal tool for deciding which jobs are worth pursuing. Core thesis: help make better decisions, not apply to more jobs.

---

## Current State (as of last session)

**App is live and practically usable.**

- 188 jobs collected (30 Jobindex, 151 LinkedIn, 7 Indeed)
- Jobs scored by Ollama phi3:mini in background worker
- Git is clean at commit `317defe` on `main`
- Repo: https://github.com/bilalsarwar2907/career-intelligence

**Daily workflow:**
1. `cd ~/CareerCopilot2/JobCollector && python collect_jobs.py`
2. `cd ~/CareerCopilot2/CareerCopilot && dotnet run`
3. Open `http://localhost:5000` → Jobs → Collect & Analyse New Jobs
4. Dashboard shows ranked opportunities — check Details, mark applied

---

## Architecture: Intelligence Copilot Framework (10 Layers)

Documented in `INTELLIGENCE_COPILOT_FRAMEWORK.md`. Career Copilot is Implementation A.

| Layer | Name | Career Copilot | Status |
|-------|------|----------------|--------|
| 1 | Profile Context | `UserProfile` (resume, skills, goals, salary, locations) | ✅ Built |
| 2 | Opportunity Context | `Job` (title, company, location, description, URL, source) | ✅ Built |
| 3 | Acquisition Layer | `collect_jobs.py` (Jobindex RSS + JobSpy) + `JobCollectorJson` | ✅ Built |
| 4 | Knowledge Layer | SQLite via EF Core (`Jobs`, `UserProfiles`, `FitAnalyses`, `ApplicationRecords`) | ✅ Built |
| 5 | Intelligence Engine | `FitAnalyzerOllama` / `FitAnalyzerOpenAI` | ✅ Built |
| 6 | Explainability Engine | `JobDetail.cshtml` — score, strengths, gaps, blockers | ✅ Built |
| 7 | Recommendation Engine | `Recommendation` enum (ApplyNow / ApplyIfInterested / Skip) + ResumeAdvice | ✅ Built |
| 8 | Human Approval | Apply / Skip on Dashboard + JobDetail | ✅ Built |
| 9 | Outcome Tracking | `ApplicationRecord` + Tracker page | ✅ Built |
| 10 | Learning Loop | Outcomes tracked, not yet fed back into AI scoring | ⚠️ Pending |

**9 of 10 layers complete.** Layer 10 (Learning Loop) should be built after 4–8 weeks of real outcome data.

---

## Tech Stack

- ASP.NET Core 8, Razor Pages, EF Core, SQLite
- Ollama (local LLM — phi3:mini) + OpenAI API (gpt-4o-mini, optional)
- JobSpy (Python — LinkedIn/Indeed scraping)
- Jobindex RSS (Danish jobs — `https://www.jobindex.dk/jobsoegning.rss`)
- BackgroundService + Channel<int> for async job analysis queue
- SemaphoreSlim(2) for concurrency control

---

## Key Files

### C# App — `CareerCopilot2/CareerCopilot/`

| File | Purpose |
|------|---------|
| `Models/Job.cs` | Raw job data: Title, Company, Location, Description, Url, Source, DateFound, IsActive |
| `Models/UserProfile.cs` | All fields `string?` nullable. Fields: Name, ResumeText, Skills, PreferredTitles, PreferredLocations, PreferredTechnologies, MinSalary, CareerGoals |
| `Models/FitAnalysis.cs` | Score (double), Strengths/Gaps/HardBlockers (JSON arrays), Explanation, Recommendation enum, ResumeAdvice, EstimatedEffortMinutes |
| `Models/ApplicationRecord.cs` | ApplicationStatus enum: Interested, Applied, PhoneScreen, Interview, Offer, Rejected, Withdrawn, Skipped. GotInterview, GotOffer booleans |
| `Data/AppDbContext.cs` | EF Core DbContext, SQLite, one-to-one Job→FitAnalysis, Job→ApplicationRecord |
| `Services/FitAnalyzerOllama.cs` | HTTP to Ollama at localhost:11434, 300s timeout, truncates CV/description, strips markdown fences |
| `Services/FitAnalyzerOpenAI.cs` | OpenAI SDK ChatClient, JSON response format |
| `Services/AnalysisQueue.cs` | `Channel<int>` unbounded channel for job IDs, `PendingCount` property |
| `Services/AnalysisWorker.cs` | BackgroundService, SemaphoreSlim(2), IServiceScopeFactory per job, skips already-analysed jobs |
| `Services/JobCollectorJson.cs` | Reads `jobs.json` from Python script. Path via `JobCollector:JsonPath` in appsettings |
| `Services/ApplicationTrackerService.cs` | Upserts ApplicationRecord, auto-sets timestamps on status transitions |
| `Pages/Index.cshtml` + `.cs` | Dashboard — top scored jobs, score colour coding, Apply button |
| `Pages/Jobs.cshtml` + `.cs` | All jobs table — collect + analyse trigger, score badges |
| `Pages/JobDetail.cshtml` + `.cs` | Score, explanation, strengths (green), gaps (yellow), blockers (red), resume advice, status dropdown. Route: `/JobDetail/{id:int}` |
| `Pages/Profile.cshtml` + `.cs` | `[BindProperty]` flat properties (NOT nested UserProfile), upserts single row |
| `Pages/Tracker.cshtml` + `.cs` | Stats: Applied, Interviews, Offers, Rejected, Interview Rate. Table with status update per row |
| `Program.cs` | DI: AnalysisQueue (Singleton), AnalysisWorker (HostedService), IJobCollector→JobCollectorJson, IFitAnalyzer→FitAnalyzerOllama, IApplicationTracker→ApplicationTrackerService. Auto-migrates DB on startup |
| `appsettings.json` | Ollama: BaseUrl `http://localhost:11434`, Model `phi3:mini`. OpenAI key = placeholder (use dotnet user-secrets). JobCollector:JsonPath pointing to jobs.json |

### Python Collector — `CareerCopilot2/JobCollector/`

| File | Purpose |
|------|---------|
| `collect_jobs.py` | Multi-source collector. Sources: Jobindex RSS (storkoebenhavn + alle areas) + LinkedIn/Indeed via JobSpy. Deduplicates across all sources. Skips jobs older than 7 days. Outputs `../CareerCopilot/jobs.json` |

**Jobindex RSS URL pattern:** `https://www.jobindex.dk/jobsoegning.rss?q=TERM&area=AREA`
- No `subid` filter (subid=12 IT category was too restrictive for English search terms)
- it-jobbank.dk (Computerworld) has no public RSS — same Jobindex infrastructure, covered by Jobindex searches
- Jobagent is a Jobindex product — same jobs, covered by Jobindex searches

**Search terms:** `.NET Developer`, `C# Developer`, `Full Stack Developer`, `Backend Developer`, `Software Engineer C#`
**Areas:** `storkoebenhavn` (Greater Copenhagen), `alle` (all Denmark)
**Dependencies:** `pip install python-jobspy requests beautifulsoup4 lxml`

### Documentation — `CareerCopilot2/`

| File | Purpose |
|------|---------|
| `INTELLIGENCE_COPILOT_FRAMEWORK.md` | 10-layer reusable framework (domain-agnostic) + Career Copilot Implementation A + Stock Agent Implementation B (planned). Replaces old APPLICATION_LEGIBILITY_PATTERN.md |
| `APPLICATION_LEGIBILITY_PATTERN.md` | Old 9-layer doc — superseded, kept for reference |
| `.github/workflows/ci.yml` | CI: checkout, dotnet 8, restore, build Release, test |
| `.gitignore` | Excludes: bin/, obj/, *.db, appsettings.Development.json, appsettings.Secrets.json |

---

## Known Bugs / Past Fixes (don't repeat these mistakes)

| Bug | Fix |
|-----|-----|
| Missing `_ViewStart.cshtml` → HTTP 500 | Added `Pages/_ViewStart.cshtml` with `@{ Layout = "_Layout"; }` |
| "no such table: Jobs" | Migration never ran. Fix: `dotnet ef migrations add InitialCreate` from inside `CareerCopilot/` directory |
| NOT NULL constraint on UserProfiles | (1) Parameter binding fails for complex types — use `[BindProperty]` flat properties. (2) Empty strings become null for non-nullable — made all `UserProfile` fields `string?` |
| OpenAI 429 quota | Switch to Ollama: `IFitAnalyzer → FitAnalyzerOllama` in Program.cs |
| JobSpy on Python 3.14 | NumPy incompatibility. Fix: `pip install numpy pandas` first, then `pip install python-jobspy --no-deps`, then install deps manually |
| Timeout with many parallel analyses | Background worker (AnalysisQueue + AnalysisWorker) — page returns instantly, analysis runs async |
| Jobs stuck "Not analysed" | Collector skipped duplicates so failed analyses never retried. Fix: analysis now runs on ALL unanalysed jobs regardless of when collected |
| Real API key committed to git | Replaced with placeholder before push. Use `dotnet user-secrets set "OpenAI:ApiKey" "YOUR_KEY"` |
| git index.lock on Windows | In Git Bash: `rm .git/index.lock`. In CMD: `del .git\index.lock` |
| Jobindex only 4 results | `subid=12` category filter was too restrictive for English search terms. Removed — now returns 30+ |
| it-jobbank.dk 404 on RSS | it-jobbank is a React SPA with no public RSS. Same infrastructure as Jobindex. Removed from collector |

---

## Pending Work

**Layer 10 — Learning Loop** (build after 4–8 weeks of real usage):
- Read `ApplicationRecord` outcomes (GotInterview, GotOffer, Rejected)
- Jobs with score >8 that got interviews → reinforce prompt weighting
- Jobs with score >8 that got rejected → adjust calibration
- Feed back into `FitAnalyzerOllama` prompt as few-shot examples

**Possible improvements to pick up if bugs/requests arise:**
- Jobagent email integration (currently manual — run collect_jobs.py daily)
- Schedule `collect_jobs.py` to run automatically (Task Scheduler / cron)
- Filter jobs by minimum score on Dashboard
- Export shortlist to PDF/email
- Add salary range field to UserProfile and FitAnalysis

---

## Run Commands

```bash
# Collect fresh jobs
cd ~/CareerCopilot2/JobCollector
python collect_jobs.py

# Start app
cd ~/CareerCopilot2/CareerCopilot
dotnet run

# Add migration (if models change)
dotnet ef migrations add MigrationName
dotnet ef database update

# Switch to OpenAI (faster than Ollama)
# In Program.cs: services.AddScoped<IFitAnalyzer, FitAnalyzerOpenAI>();
# Set key: dotnet user-secrets set "OpenAI:ApiKey" "sk-..."
```
