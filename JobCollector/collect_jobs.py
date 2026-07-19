"""
Career Intelligence — Job Collector
Uses JobSpy to fetch jobs from LinkedIn and Indeed.
Outputs jobs.json for the C# app to read.

Usage:
    pip install python-jobspy
    python collect_jobs.py
"""

import json
import csv
import io
from datetime import datetime
from jobspy import scrape_jobs

SEARCH_TERMS = [
    ".NET Developer",
    "Full Stack Developer",
    "Backend Developer C#",
]

LOCATIONS = ["Copenhagen, Denmark", "Denmark"]
REMOTE_ONLY = True   # also collect remote roles

RESULTS_PER_SEARCH = 20
OUTPUT_FILE = "../CareerCopilot/jobs.json"


def collect():
    all_jobs = []
    seen_keys = set()

    for term in SEARCH_TERMS:
        for location in LOCATIONS:
            print(f"Searching: '{term}' in '{location}' ...")
            try:
                jobs = scrape_jobs(
                    site_name=["linkedin", "indeed"],
                    search_term=term,
                    location=location,
                    results_wanted=RESULTS_PER_SEARCH,
                    hours_old=72,          # last 3 days
                    country_indeed="Denmark",
                )
                for _, row in jobs.iterrows():
                    key = f"{row.get('title', '')}|{row.get('company', '')}"
                    if key in seen_keys:
                        continue
                    seen_keys.add(key)

                    all_jobs.append({
                        "title":       str(row.get("title", "")).strip(),
                        "company":     str(row.get("company", "")).strip(),
                        "location":    str(row.get("location", "")).strip(),
                        "description": str(row.get("description", "")).strip(),
                        "url":         str(row.get("job_url", "")).strip(),
                        "source":      str(row.get("site", "")).strip().title(),
                    })
            except Exception as e:
                print(f"  Error: {e}")

        # Also search remote roles
        if REMOTE_ONLY:
            print(f"Searching: '{term}' remote ...")
            try:
                jobs = scrape_jobs(
                    site_name=["linkedin", "indeed"],
                    search_term=term,
                    is_remote=True,
                    results_wanted=RESULTS_PER_SEARCH,
                    hours_old=72,
                    country_indeed="Denmark",
                )
                for _, row in jobs.iterrows():
                    key = f"{row.get('title', '')}|{row.get('company', '')}"
                    if key in seen_keys:
                        continue
                    seen_keys.add(key)

                    all_jobs.append({
                        "title":       str(row.get("title", "")).strip(),
                        "company":     str(row.get("company", "")).strip(),
                        "location":    str(row.get("location", "Remote")).strip(),
                        "description": str(row.get("description", "")).strip(),
                        "url":         str(row.get("job_url", "")).strip(),
                        "source":      str(row.get("site", "")).strip().title(),
                    })
            except Exception as e:
                print(f"  Error: {e}")

    # Write output
    output = {
        "collected_at": datetime.utcnow().isoformat(),
        "count": len(all_jobs),
        "jobs": all_jobs,
    }

    with open(OUTPUT_FILE, "w", encoding="utf-8") as f:
        json.dump(output, f, ensure_ascii=False, indent=2)

    print(f"\nDone. {len(all_jobs)} jobs written to {OUTPUT_FILE}")


if __name__ == "__main__":
    collect()
