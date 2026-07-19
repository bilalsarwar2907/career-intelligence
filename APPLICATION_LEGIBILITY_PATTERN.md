# The Application Legibility Pattern
## Intelligence Copilot Framework

> Expose the live state of an application through a local legibility layer, transform that state into structured data, let AI reason over the structured data, and keep the human as the final decision-maker.

---

## The Problem with Traditional AI Workflows

```
Application → API or Scraper → Raw Data → AI
```

| Problem | Why it hurts |
|--------|-------------|
| APIs are expensive | Cost scales with usage |
| APIs are limited | Rate limits, restricted fields |
| Scrapers break | Sites change layout constantly |
| Anti-bot protection | Blocks automated access |
| Website changes | Maintenance burden |

---

## The 9-Layer Intelligence Framework

Every intelligence copilot — career, trading, sales, procurement — follows the same 9 layers.

```
Layer 1 — Profile Context
         ↓
Layer 2 — Opportunity Context
         ↓
Layer 3 — Legibility Layer
         ↓
Layer 4 — Intelligence Engine
         ↓
Layer 5 — Explainability Engine
         ↓
Layer 6 — Recommendation Engine
         ↓
Layer 7 — Human Approval
         ↓
Layer 8 — Outcome Tracking
         ↓
Layer 9 — Learning Loop
```

---

## Layer Definitions

### Layer 1 — Profile Context
*Who am I? What are my goals? What are my constraints?*

The persistent model of the user's identity, preferences, and constraints. Everything the AI needs to personalise its reasoning.

| Career Copilot | Stock Agent |
|---------------|-------------|
| Resume, Skills, Career Goals | Risk Profile, Portfolio |
| Preferred Titles, Locations | Trading Rules, Capital |
| Min Salary, Technologies | Target Return, Max Drawdown |

**In code:** `UserProfile` model, Profile page.

---

### Layer 2 — Opportunity Context
*What opportunity exists right now?*

The raw data about a single opportunity — not yet scored, not yet reasoned about.

| Career Copilot | Stock Agent |
|---------------|-------------|
| Job Title, Company, Location | Ticker, Price, Volume |
| Description, Salary | Chart pattern, Indicators |
| Source (LinkedIn, Indeed) | Market conditions |

**In code:** `Job` model, `JobCollectorJson`, SQLite `Jobs` table.

---

### Layer 3 — Legibility Layer
*Extract state from external systems. Convert chaos to structured data.*

The bridge between the outside world and the intelligence engine. Its only job is to observe and normalise.

**Responsibilities:** Observe → Extract → Normalise

```json
{
  "title": "Senior .NET Developer",
  "company": "Novo Nordisk",
  "location": "Copenhagen",
  "skills_required": ["C#", ".NET", "Azure", "Docker"],
  "salary": 65000
}
```

**Possible implementations:**

| Source | Bridge |
|--------|--------|
| Web app (SPA) | Chrome DevTools Protocol (CDP) |
| Web app (HTML) | HTTP scraper (JobSpy, BeautifulSoup) |
| Desktop app | Accessibility APIs (UIAutomation) |
| REST API | Direct API client |
| Database | SQL / ORM query |
| File export | CSV / JSON file watcher |

> CDP is one implementation. The pattern is the idea.

**In code:** `JobCollectorJson` reads `jobs.json` from `collect_jobs.py` (JobSpy).

---

### Layer 4 — Intelligence Engine
*Analyse opportunity against profile. Produce a score.*

The AI reasoning layer. Takes structured opportunity + structured profile and returns a numerical assessment.

| Career Copilot | Stock Agent |
|---------------|-------------|
| Fit Score (0–10) | Trade Score (0–10) |
| Skills match | Signal strength |
| Experience match | Risk/reward ratio |

**In code:** `IFitAnalyzer` / `FitAnalyzerOllama` / `FitAnalyzerOpenAI`, `FitAnalysis` model.

---

### Layer 5 — Explainability Engine
*Why this score? Strengths, risks, gaps — in plain language.*

The most important layer for building user trust. A score without explanation is useless.

| Career Copilot | Stock Agent |
|---------------|-------------|
| Why 9.0? | Why Buy? |
| ✓ C#, ✓ Azure, ✓ REST APIs | ✓ RSI oversold, ✓ Support level |
| ✗ Docker, ✗ Kubernetes | ✗ Earnings next week, ✗ Downtrend |
| Hard blockers (clearance, etc.) | Hard blockers (earnings, news) |

**In code:** `FitAnalysis.Strengths`, `.Gaps`, `.HardBlockers`, `.Explanation` stored as JSON. Rendered in `JobDetail.cshtml` as coloured tags.

---

### Layer 6 — Recommendation Engine
*What should the human do? Be specific.*

Converts the score and explanation into a concrete, actionable recommendation.

| Career Copilot | Stock Agent |
|---------------|-------------|
| ApplyNow | Buy |
| ApplyIfInterested | Watch |
| Skip | Avoid |
| Resume Advice | Position size, Stop loss |

**In code:** `Recommendation` enum, `FitAnalysis.ResumeAdvice`, rendered on Dashboard and JobDetail.

---

### Layer 7 — Human Approval
*Final decision always stays with the human.*

The AI recommends. The human decides. Never remove this layer.

| Career Copilot | Stock Agent |
|---------------|-------------|
| Apply / Skip buttons | Execute / Hold buttons |
| Mark Applied | Place order manually |
| Save for later | Add to watchlist |

**In code:** Apply button on Dashboard and JobDetail, status dropdown in Tracker.

---

### Layer 8 — Outcome Tracking
*Record what happened. Most projects skip this. Don't.*

The feedback data that makes Layer 9 possible. Without outcomes, the AI never learns.

| Career Copilot | Stock Agent |
|---------------|-------------|
| Applied → Interview → Offer → Rejected | Signal → Trade → Win / Loss |
| Interview rate | Win rate |
| Time to offer | Profit factor |

**In code:** `ApplicationRecord` model, `ApplicationTrackerService`, Tracker page with stats (Applied / Interviews / Offers / Rejected / Interview Rate).

---

### Layer 9 — Learning Loop
*Use outcomes to improve future recommendations.*

The layer that separates a static tool from an intelligent one. Feed outcomes back into the scoring model to make future recommendations more accurate.

```
Recommendation → Human Decision → Outcome → Better Recommendations
```

| Career Copilot | Stock Agent |
|---------------|-------------|
| Jobs with score >8 that got interviews → reinforce prompt | Signals that hit target → reinforce pattern |
| Jobs with score >8 that got rejected → adjust calibration | Signals that stopped out → penalise pattern |

**In code:** ⚠️ **NOT YET BUILT** — outcomes are tracked but not yet fed back into scoring. This is the next meaningful feature after the MVP is validated.

---

## Career Copilot — Layer Implementation Status

| Layer | Name | Status | Code |
|-------|------|--------|------|
| 1 | Profile Context | ✅ Complete | `UserProfile`, `Profile.cshtml` |
| 2 | Opportunity Context | ✅ Complete | `Job`, `Jobs` table |
| 3 | Legibility Layer | ✅ Complete | `JobCollectorJson` + `collect_jobs.py` |
| 4 | Intelligence Engine | ✅ Complete | `FitAnalyzerOllama` / `FitAnalyzerOpenAI` |
| 5 | Explainability Engine | ✅ Complete | `JobDetail.cshtml` — strengths, gaps, explanation |
| 6 | Recommendation Engine | ✅ Complete | `Recommendation` enum, resume advice |
| 7 | Human Approval | ✅ Complete | Apply / Skip on Dashboard + JobDetail |
| 8 | Outcome Tracking | ✅ Complete | `ApplicationRecord`, Tracker page |
| 9 | Learning Loop | ⚠️ Not built | Outcomes tracked, not yet fed back into AI |

**8 of 9 layers are live.** The only missing layer is the Learning Loop — and that requires real outcome data first, which means you need to use the tool for a few weeks before it can be built meaningfully.

---

## The Pattern Applied to Other Domains

| Domain | Layer 1 (Profile) | Layer 2 (Opportunity) | Layer 6 (Recommendation) |
|--------|------------------|----------------------|--------------------------|
| Career Intelligence | Resume + Goals | Job Description | Apply / Skip |
| Trading Intelligence | Risk Profile + Rules | Chart + Indicators | Buy / Sell / Hold |
| CRM Intelligence | Sales Playbook | Customer Record | Next Best Action |
| Procurement | Supplier Criteria | Supplier Quote | Approve / Reject |
| Project Management | Team + Budget | Project Status | Escalate / Continue |

Only Layers 1, 2, 3, and 6 change between domains. Layers 4, 5, 7, 8, 9 are identical in every implementation.

---

## Generic Component Model (for any new agent)

```
Connector          ← Layer 3: reads the external source
    ↓
Extractor          ← Layer 3: pulls relevant fields
    ↓
Normalizer         ← Layer 3: converts to structured JSON
    ↓
Knowledge Store    ← Layer 2+8: SQLite (Jobs + Outcomes)
    ↓
AI Reasoner        ← Layer 4: score against profile
    ↓
Explainer          ← Layer 5: why this score
    ↓
Recommender        ← Layer 6: what to do
    ↓
Human Approval     ← Layer 7: final decision
    ↓
Outcome Tracker    ← Layer 8: record result
    ↓
Learning Loop      ← Layer 9: improve next time
```

---

## Implementation Checklist (for any new agent)

- [ ] Define **Layer 1**: what does the user profile look like?
- [ ] Define **Layer 2**: what is an "opportunity" in this domain?
- [ ] Choose **Layer 3** bridge technology (CDP / scraper / API / file)
- [ ] Define the **normalised data model** (JSON shape the AI receives)
- [ ] Build **Layer 4**: AI reasoning prompt (score + structured output)
- [ ] Build **Layer 5**: explainability UI (strengths / gaps / blockers)
- [ ] Build **Layer 6**: recommendation display (what action to take)
- [ ] Build **Layer 7**: human approval UI (one-click decision)
- [ ] Build **Layer 8**: outcome tracker (record what happened)
- [ ] Build **Layer 9**: learning loop (after real data accumulates)

---

## The Most Valuable Insight

Career Copilot is not a job application tool. It is a proof of concept for the Intelligence Copilot Framework — a 9-layer architecture that can power any domain where a human needs help making better decisions about opportunities.

The same framework powers:
- Career Copilot (built)
- Stock Copilot (next)
- Sales Copilot
- Procurement Copilot
- Project Copilot

Only the Profile and Opportunity models change. Everything else is the same code.
