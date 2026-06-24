#!/usr/bin/env python3
"""Transforma relatório JSON do Playwright em formato simplificado para o frontend."""
from __future__ import annotations

import json
import sys
from datetime import datetime, timezone
from pathlib import Path


def flatten_specs(suite: dict, prefix: str = "") -> list[dict]:
    rows: list[dict] = []
    title = suite.get("title", "")
    name = f"{prefix}{title}".strip(" ›")

    for spec in suite.get("specs", []):
        spec_title = spec.get("title", "")
        full_name = f"{name} › {spec_title}".strip(" ›") if name else spec_title
        for test in spec.get("tests", []):
            results = test.get("results", [])
            status = results[-1].get("status", "unknown") if results else "unknown"
            duration = sum(r.get("duration", 0) for r in results)
            rows.append(
                {
                    "name": full_name,
                    "status": status,
                    "durationMs": round(duration),
                }
            )

    for child in suite.get("suites", []):
        rows.extend(flatten_specs(child, f"{name} › " if name else ""))

    return rows


def main() -> int:
    if len(sys.argv) != 3:
        print("Usage: transform-e2e-results.py <playwright-report.json> <output.json>", file=sys.stderr)
        return 1

    src = Path(sys.argv[1])
    dst = Path(sys.argv[2])

    if not src.exists():
        dst.write_text(
            json.dumps(
                {
                    "status": "pending",
                    "message": "Execute ./scripts/run-quality-gates.sh com a stack rodando.",
                    "generatedAt": None,
                    "total": 0,
                    "passed": 0,
                    "failed": 0,
                    "durationMs": 0,
                    "scenarios": [],
                },
                indent=2,
                ensure_ascii=False,
            )
            + "\n",
            encoding="utf-8",
        )
        return 0

    data = json.loads(src.read_text(encoding="utf-8"))
    scenarios = flatten_specs(data) if "suites" in data else []
    if not scenarios and "suites" in data:
        for suite in data["suites"]:
            scenarios.extend(flatten_specs(suite))

    passed = sum(1 for s in scenarios if s["status"] == "passed")
    failed = sum(1 for s in scenarios if s["status"] == "failed")
    total = len(scenarios)
    duration_ms = round(data.get("stats", {}).get("duration", 0))

    payload = {
        "status": "failed" if failed else ("passed" if total else "pending"),
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "total": total,
        "passed": passed,
        "failed": failed,
        "durationMs": duration_ms,
        "scenarios": scenarios,
    }

    dst.parent.mkdir(parents=True, exist_ok=True)
    dst.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Wrote {dst} — {passed}/{total} E2E passed")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
