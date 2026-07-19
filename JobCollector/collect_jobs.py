"""
Career Intelligence — Job Collector
Sources:
  - LinkedIn (via JobSpy)
  - Jobindex.dk (RSS, IT category — also covers Jobagent and Computerworld IT-job)
  - it-jobbank.dk (RSS — Computerworld's job board engine)

Usage:
    pip install python-jobspy
    pip install requests beautifulsoup4   (for RSS parsing)
    python collect_jobs.py
"""

import json
import re
import time
from datetime import datetime, timedelta, timezone

import requests
from bs4 import BeautifulSoup

try:
    from jobspy import scrape_jobs
    JOBSPY_AVAILABLE = True
except ImportError:
    print("Warning: python-jobspy not installed. LinkedIn/Indeed disabled.")
    JOBSPY_AVAILABLE = False

# ─── CONFIG ──────────────────────────────────────────────────────────────────

SEARCH_TERMS = [
    ".NET Developer",
    "C# Developer",
    "Full Stack Developer",
    "Backend Developer",
    "Software Engineer C#",
]

JOBINDEX_AREA = "storkoebenhavn"   # Greater Copenhagen; use "alle" for all Denmark

RESULTS_PER_SEARCH = 20            # LinkedIn / Indeed limit per query
MAX_AGE_DAYS       = 7             # skip jobs older than this
OUTPUT_FILE        = "../CareerCopilot/jobs.json"

# Jobindex subid=12 = IT & telecommunications category
JOBINDEX_RSS_BASE  = "https://www.jobindex.dk/jobsoegning.rss"
IT_JOBBANK_RSS_BASE = "https://www.it-jobbank.dk/jobsoegning.rss"

HEADERS = {
    "User-Agent": (
        "Mozilla/5.0 (Windows NT 10.0; Win64; x64) "
        "AppleWebKit/537.36 (KHTML, like Gecko) "
        "Chrome/124.0.0.0 Safari/537.36"
    ),
    "Accept": "application/rss+xml, application/xml, text/xml, */*",
    "Accept-Language": "da-DK,da;q=0.9,en;q=0.8",
}

# ─── HELPERS ─────────────────────────────────────────────────────────────────

def strip_html(html: str) -> str:
    """Remove HTML tags and decode entities."""
    soup = BeautifulSoup(html, "html.parser")
    return soup.get_text(separator=" ", strip=True)


def parse_jobindex_title(raw_title: str):
    """
    Jobindex RSS titles are formatted as:
        "Job Title, Company Name"
    Split on the LAST comma to separate them.
    """
    parts = raw_title.rsplit(",", 1)
    if len(parts) == 2:
        return parts[0].strip(), parts[1].strip()
    return raw_title.strip(), ""


def extract_jobindex_location(description_html: str) -> str:
    """Extract location from the jix_robotjob--area span in the description."""
    soup = BeautifulSoup(description_html, "html.parser")
    span = soup.find("span", class_="jix_robotjob--area")
    if span:
        return span.get_text(strip=True)
    return "Denmark"


def is_too_old(pub_date_str: str, max_days: int) -> bool:
    """Return True if the job is older than max_days. Returns False on parse error."""
    if not pub_date_str:
        return False
    try:
        from email.utils import parsedate_to_datetime
        pub = parsedate_to_datetime(pub_date_str)
        cutoff = datetime.now(timezone.utc) - timedelta(days=max_days)
        return pub < cutoff
    except Exception:
        return False


# ─── JOBINDEX / IT-JOBBANK RSS SCRAPER ───────────────────────────────────────

def collect_from_rss(base_url: str, source_label: str,
                     search_terms: list, area: str,
                     seen_keys: set, max_days: int) -> list:
    jobs = []

    for term in search_terms:
        params = {
            "q":      term,
            "area":   area,
            "subid":  12,         # IT & telecommunications category
        }
        url = base_url + "?" + "&".join(f"{k}={requests.utils.quote(str(v))}" for k, v in params.items())
        print(f"  [{source_label}] Searching: '{term}' ...")

        try:
            resp = requests.get(url, headers=HEADERS, timeout=20)
            resp.raise_for_status()
            # Jobindex serves ISO-8859-1; requests may misdetect — force UTF-8 fallback
            content = resp.content.decode("iso-8859-1", errors="replace")
        except Exception as e:
            print(f"    Error fetching RSS: {e}")
            continue

        try:
            soup = BeautifulSoup(content, "xml")
        except Exception:
            soup = BeautifulSoup(content, "html.parser")

        items = soup.find_all("item")
        if not items:
            print(f"    No items returned.")
            continue

        for item in items:
            title_raw   = (item.find("title") or {}).get_text(strip=True) if item.find("title") else ""
            link        = (item.find("link")  or {}).get_text(strip=True) if item.find("link")  else ""
            desc_raw    = (item.find("description") or {}).get_text(strip=True) if item.find("description") else ""
            pub_date    = (item.find("pubDate") or {}).get_text(strip=True) if item.find("pubDate") else ""
            guid_tag    = item.find("guid")
            guid        = guid_tag.get_text(strip=True) if guid_tag else link

            if is_too_old(pub_date, max_days):
                continue

            title, company = parse_jobindex_title(title_raw)
            location       = extract_jobindex_location(desc_raw)
            description    = strip_html(desc_raw)

            # Deduplicate by guid first, then title+company
            dedup_key = guid or f"{title}|{company}"
            if dedup_key in seen_keys:
                continue
            seen_keys.add(dedup_key)

            if not title:
                continue

            jobs.append({
                "title":       title,
                "company":     company,
                "location":    location,
                "description": description,
                "url":         link,
                "source":      source_label,
            })

        time.sleep(1.5)   # be polite

    print(f"  [{source_label}] {len(jobs)} unique jobs collected.")
    return jobs


# ─── LINKEDIN / INDEED via JOBSPY ────────────────────────────────────────────

def collect_from_jobspy(seen_keys: set) -> list:
    if not JOBSPY_AVAILABLE:
        return []

    jobs = []
    LOCATIONS  = ["Copenhagen, Denmark", "Denmark"]

    for term in SEARCH_TERMS:
        for location in LOCATIONS:
            print(f"  [LinkedIn/Indeed] Searching: '{term}' in '{location}' ...")
            try:
                df = scrape_jobs(
                    site_name=["linkedin", "indeed"],
                    search_term=term,
                    location=location,
                    results_wanted=RESULTS_PER_SEARCH,
                    hours_old=MAX_AGE_DAYS * 24,
                    country_indeed="Denmark",
                )
                jobs += _df_to_jobs(df, seen_keys)
            except Exception as e:
                print(f"    Error: {e}")

        # Remote search
        print(f"  [LinkedIn/Indeed] Searching: '{term}' remote ...")
        try:
            df = scrape_jobs(
                site_name=["linkedin", "indeed"],
                search_term=term,
                is_remote=True,
                results_wanted=RESULTS_PER_SEARCH,
                hours_old=MAX_AGE_DAYS * 24,
                country_indeed="Denmark",
            )
            jobs += _df_to_jobs(df, seen_keys, default_location="Remote")
        except Exception as e:
            print(f"    Error: {e}")

    print(f"  [LinkedIn/Indeed] {len(jobs)} unique jobs collected.")
    return jobs


def _df_to_jobs(df, seen_keys: set, default_location: str = "") -> list:
    jobs = []
    for _, row in df.iterrows():
        key = f"{row.get('title', '')}|{row.get('company', '')}"
        if key in seen_keys:
            continue
        seen_keys.add(key)
        jobs.append({
            "title":       str(row.get("title",       "")).strip(),
            "company":     str(row.get("company",     "")).strip(),
            "location":    str(row.get("location",    default_location)).strip(),
            "description": str(row.get("description", "")).strip(),
            "url":         str(row.get("job_url",     "")).strip(),
            "source":      str(row.get("site",        "")).strip().title(),
        })
    return jobs


# ─── MAIN ────────────────────────────────────────────────────────────────────

def collect():
    all_jobs  = []
    seen_keys: set = set()

    print("\n=== Jobindex (Danish IT jobs) ===")
    all_jobs += collect_from_rss(
        base_url=JOBINDEX_RSS_BASE,
        source_label="Jobindex",
        search_terms=SEARCH_TERMS,
        area=JOBINDEX_AREA,
        seen_keys=seen_keys,
        max_days=MAX_AGE_DAYS,
    )

    print("\n=== IT-Jobbank / Computerworld IT-job ===")
    all_jobs += collect_from_rss(
        base_url=IT_JOBBANK_RSS_BASE,
        source_label="Computerworld",
        search_terms=SEARCH_TERMS,
        area=JOBINDEX_AREA,
        seen_keys=seen_keys,
        max_days=MAX_AGE_DAYS,
    )

    print("\n=== LinkedIn & Indeed ===")
    all_jobs += collect_from_jobspy(seen_keys)

    # Write output
    output = {
        "collected_at": datetime.utcnow().isoformat(),
        "count":        len(all_jobs),
        "jobs":         all_jobs,
    }
    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        json.dump(output, f, ensure_ascii=False, indent=2)

    # Summary
    by_source: dict = {}
    for j in all_jobs:
        by_source[j["source"]] = by_source.get(j["source"], 0) + 1

    print(f"\n✓ Done. {len(all_jobs)} total jobs written to {OUTPUT_FILE}")
    print("  By source:")
    for src, count in sorted(by_source.items()):
        print(f"    {src}: {count}")


if __name__ == "__main__":
    collect()
