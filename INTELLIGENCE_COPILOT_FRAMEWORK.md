# Intelligence Copilot Framework v1

> A reusable 10-layer architecture for building decision-support products that combine structured data acquisition, AI reasoning, and human approval.

---

## The Core Idea

Traditional AI workflows force AI to navigate software directly — scraping, clicking, parsing. This is fragile.

The Intelligence Copilot Framework inverts this: **expose the software's state to the AI, not the AI to the software.**

```
External World
      ↓
Acquisition Layer     ← structured context enters here
      ↓
Knowledge Layer       ← context is stored and enriched
      ↓
Intelligence Engine   ← AI reasons over structured data
      ↓
Human Decision        ← human stays in control
      ↓
Outcome Tracking      ← results are recorded
      ↓
Learning Loop         ← system improves over time
```

---

## The 10 Layers

### Layer 1 — Profile Context
**Who is the user? What are their goals and constraints?**

The persistent model of the user's identity, preferences, and decision criteria. The AI uses this to personalise every recommendation.

Must define:
- Identity (who they are)
- Goals (what they want to achieve)
- Constraints (what they cannot or will not accept)
- Preferences (what they optimise for)

---

### Layer 2 — Opportunity Context
**What opportunity exists right now?**

The raw data about a single candidate item — not yet scored, not yet reasoned about. Comes from Layer 3.

Must define:
- What counts as an "opportunity" in this domain
- What fields are required for scoring
- What fields are optional

---

### Layer 3 — Acquisition Layer
**Acquire, normalise, and deliver structured context from external systems.**

Previously called "Legibility Layer." Renamed because it is not just about making things legible — it is about acquiring context from any source and delivering it in a normalised form.

Responsibilities: **Acquire → Normalise → Deliver**

Output is always clean, structured JSON. The format is defined by Layer 2.

Possible implementations:
| Source | Technology |
|--------|-----------|
| Web app (SPA) | Chrome DevTools Protocol (CDP) |
| Web app (HTML) | HTTP scraper |
| Desktop app | Accessibility APIs |
| REST API | API client |
| Database | SQL / ORM |
| File | CSV / JSON watcher |
| Message queue | MQ consumer |
| Webhook | Event listener |

The technology evolves. The layer's responsibility does not.

---

### Layer 4 — Knowledge Layer
**Store, enrich, and retrieve structured context for AI reasoning.**

This layer holds everything the AI needs to reason well — not just the current opportunity, but historical opportunities, past outcomes, and the user profile. It transforms isolated data points into a connected knowledge base.

Without this layer: AI sees one job, one chart, one record.
With this layer: AI sees the current opportunity *in context* of everything that came before.

Contents:
- Current and historical opportunities
- User profile
- Past AI analyses
- Outcome records
- Any enrichment data (salary benchmarks, market context, etc.)

Technology: SQLite for MVP. PostgreSQL when scale demands it.

---

### Layer 5 — Intelligence Engine
**Analyse the opportunity against the profile. Produce a score.**

The AI reasoning layer. Receives structured opportunity + user profile from the Knowledge Layer. Returns a numerical assessment with supporting evidence.

Must produce:
- A score (0–10 or equivalent)
- Evidence supporting the score
- Gaps or risks identified
- Hard blockers (deal-breakers)

Technology: any LLM (local or cloud) with structured JSON output.

---

### Layer 6 — Explainability Engine
**Why this score? Strengths, risks, gaps — in plain language.**

The most important layer for building user trust. A score without explanation is useless. A score with a clear explanation is a decision-support tool.

Must produce:
- Plain-language explanation of the score
- Positive signals (strengths)
- Negative signals (gaps / risks)
- Hard blockers highlighted separately
- Actionable advice specific to this opportunity

---

### Layer 7 — Recommendation Engine
**What should the human do? Be specific and opinionated.**

Converts score + explanation into a concrete, actionable recommendation. Should be specific enough that the user can act on it immediately.

Must produce:
- A discrete action recommendation (not just a score)
- Supporting advice for executing that action
- Estimated effort or cost of the action

---

### Layer 8 — Human Approval
**Final decision always stays with the human.**

The AI recommends. The human decides. This layer must never be removed or bypassed. It is what separates a decision-support tool from an autonomous agent.

Characteristics:
- One-click to approve or reject
- Option to modify before acting
- Option to defer (save for later)
- No action taken without explicit human confirmation

---

### Layer 9 — Outcome Tracking
**Record what happened. This is the data that makes Layer 10 possible.**

Most projects skip this layer. This is a mistake. Without outcome data, the system never learns and recommendations never improve.

Must record:
- What was recommended
- What the human decided
- What actually happened
- Key metrics (time, cost, result quality)

---

### Layer 10 — Learning Loop
**Use outcome data to improve future recommendations.**

The layer that separates a static tool from an intelligent one. Feeds outcome data back into Layer 5 (Intelligence Engine) to calibrate future scoring.

Cannot be built until Layer 9 has accumulated real data.

```
Layer 7 (Human Decision)
        ↓
Layer 9 (Outcome Tracking)
        ↓
Layer 10 (Learning Loop)
        ↓
Layer 5 (Intelligence Engine) ← improved
```

---

## Implementation Rules

1. **Build in order** — each layer depends on the one before it.
2. **Layer 9 before Layer 10** — never build the learning loop before you have real outcome data.
3. **Layer 3 is pluggable** — the acquisition technology should be swappable without changing any other layer.
4. **Layer 4 is always SQLite first** — upgrade only when data volume demands it.
5. **Layer 8 is non-negotiable** — human approval must exist at every stage of the product lifecycle.
6. **Only Layers 1, 2, 3, and 7 change between domains** — the rest are identical in every implementation.

---

## Implementation Checklist (for any new project)

- [ ] Layer 1: Define the user profile model
- [ ] Layer 2: Define what an "opportunity" is in this domain
- [ ] Layer 3: Choose acquisition technology, define normalised JSON output
- [ ] Layer 4: Set up SQLite schema (profile + opportunities + analyses + outcomes)
- [ ] Layer 5: Write the AI reasoning prompt with structured JSON output
- [ ] Layer 6: Build explainability UI (score + strengths + gaps + blockers)
- [ ] Layer 7: Build recommendation display (specific action + advice)
- [ ] Layer 8: Build human approval UI (one-click approve / reject / defer)
- [ ] Layer 9: Build outcome tracker (record decisions and results)
- [ ] Layer 10: Build learning loop (after real data accumulates — not before)

---

---
---

# Career Copilot — Framework Implementation

Career Copilot is **Implementation A** of the Intelligence Copilot Framework.

## Layer Mapping

| Layer | Framework Name | Career Copilot Implementation | Status |
|-------|---------------|-------------------------------|--------|
| 1 | Profile Context | `UserProfile` (resume, skills, goals, salary, locations) | ✅ Built |
| 2 | Opportunity Context | `Job` (title, company, location, description, URL, source) | ✅ Built |
| 3 | Acquisition Layer | `collect_jobs.py` (JobSpy → JSON) + `JobCollectorJson` (C# reader) | ✅ Built |
| 4 | Knowledge Layer | SQLite via EF Core (`Jobs`, `UserProfiles`, `FitAnalyses`, `ApplicationRecords`) | ✅ Built |
| 5 | Intelligence Engine | `FitAnalyzerOllama` / `FitAnalyzerOpenAI` → score + strengths + gaps | ✅ Built |
| 6 | Explainability Engine | `JobDetail.cshtml` — score, explanation, strengths (green), gaps (yellow), blockers (red) | ✅ Built |
| 7 | Recommendation Engine | `Recommendation` enum (ApplyNow / ApplyIfInterested / Skip) + `ResumeAdvice` | ✅ Built |
| 8 | Human Approval | Apply / Skip buttons on Dashboard and JobDetail, status dropdown | ✅ Built |
| 9 | Outcome Tracking | `ApplicationRecord` (Applied → Interview → Offer → Rejected) + Tracker stats | ✅ Built |
| 10 | Learning Loop | Outcomes tracked but not yet fed back into scoring | ⚠️ Pending |

**Score: 9 of 10 layers complete.**

The only missing layer (Learning Loop) requires real outcome data to build meaningfully. Use the tool for 4–8 weeks, then build it.

## Architecture Diagram

```
UserProfile (Layer 1)
       +
Job (Layer 2)
       ↓
collect_jobs.py → JobSpy → LinkedIn / Indeed
       ↓
jobs.json
       ↓
JobCollectorJson (Layer 3 — Acquisition)
       ↓
SQLite (Layer 4 — Knowledge)
  ├── Jobs
  ├── UserProfiles
  ├── FitAnalyses
  └── ApplicationRecords
       ↓
FitAnalyzerOllama / OpenAI (Layer 5 — Intelligence)
       ↓
JobDetail.cshtml — Strengths / Gaps / Explanation (Layer 6 — Explainability)
       ↓
ApplyNow / Skip + Resume Advice (Layer 7 — Recommendation)
       ↓
Apply Button (Layer 8 — Human Approval)
       ↓
ApplicationRecord (Layer 9 — Outcome Tracking)
       ↓
[Learning Loop — Layer 10 — Future]
```

---

---
---

# Stock Intelligence Agent — Framework Implementation (Planned)

Stock Agent is **Implementation B** of the Intelligence Copilot Framework. The framework is identical. Only Layers 1, 2, 3, and 7 change.

## Layer Mapping

| Layer | Framework Name | Stock Agent Implementation |
|-------|---------------|---------------------------|
| 1 | Profile Context | `TradingProfile` (risk tolerance, capital, rules, goals) |
| 2 | Opportunity Context | `TradeSetup` (ticker, price, indicators, market context) |
| 3 | Acquisition Layer | CDP reads TradingView DOM → normalised JSON |
| 4 | Knowledge Layer | SQLite (`TradeSetups`, `TradingProfile`, `TradeAnalyses`, `TradeHistory`) |
| 5 | Intelligence Engine | `TradeAnalyzerOllama` → score + bullish/bearish factors |
| 6 | Explainability Engine | Score + Bull Factors (green) + Bear Factors (red) + Risk Warning |
| 7 | Recommendation Engine | Buy / Sell / Hold + Position Size + Stop Loss |
| 8 | Human Approval | Execute / Skip buttons — human places the order |
| 9 | Outcome Tracking | `TradeRecord` (Entered → Target Hit / Stopped Out / Still Open) |
| 10 | Learning Loop | Signals that led to wins → reinforce; losses → penalise |

Layers 4, 5, 6, 8, 9, 10 are **direct code reuse** from Career Copilot with renamed models.

---

## The Most Valuable Insight

Career Copilot and Stock Agent are not two separate projects. They are two implementations of the same framework.

**What changes between implementations:**
- The Profile model (resume vs trading rules)
- The Opportunity model (job vs trade setup)
- The Acquisition technology (JobSpy vs CDP)
- The Recommendation labels (Apply vs Buy)

**What stays the same:**
- The Knowledge Layer (SQLite schema pattern)
- The Intelligence Engine (LLM + structured JSON prompt)
- The Explainability Engine (strengths / risks / blockers)
- The Human Approval pattern
- The Outcome Tracking schema pattern
- The Learning Loop mechanism

Build the framework once. Reuse it everywhere.
