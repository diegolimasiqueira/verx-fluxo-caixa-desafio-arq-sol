#!/usr/bin/env python3
"""Gera frontend/public/test-metrics.json a partir de TRX e cobertura Cobertura."""
from __future__ import annotations

import json
import sys
import xml.etree.ElementTree as ET
from datetime import datetime, timezone
from glob import glob
from pathlib import Path

SERVICES = [
    ("bff", "BFF", "CashFlow.Bff.Api"),
    ("launch", "Launch Service", "CashFlow.LaunchService.Api"),
    ("balance", "Daily Balance Service", "CashFlow.DailyBalanceService.Api"),
    ("worker", "Daily Balance Worker", "CashFlow.DailyBalanceWorker"),
]


def find_trx(coverage_dir: Path) -> Path | None:
    files = sorted(coverage_dir.glob("**/*.trx"), key=lambda p: p.stat().st_mtime)
    return files[-1] if files else None


def parse_trx(path: Path) -> tuple[int, int, int]:
    root = ET.parse(path).getroot()
    for elem in root.iter():
        if elem.tag.endswith("Counters"):
            total = int(elem.get("total", 0))
            passed = int(elem.get("passed", 0))
            failed = int(elem.get("failed", 0))
            return total, passed, failed
    return 0, 0, 0


def find_cobertura(coverage_dir: Path) -> Path | None:
    files = sorted(
        (p for p in coverage_dir.glob("**/coverage.cobertura.xml") if "_fedora_" not in p.parts),
        key=lambda p: p.stat().st_mtime,
    )
    return files[-1] if files else None


def parse_cobertura(path: Path) -> tuple[float, float]:
    root = ET.parse(path).getroot()
    line = round(float(root.get("line-rate", 0)) * 100, 1)
    branch = round(float(root.get("branch-rate", 0)) * 100, 1)
    return line, branch


def main() -> int:
    if len(sys.argv) != 3:
        print("Usage: generate-test-metrics.py <coverage-results-dir> <output-json>", file=sys.stderr)
        return 1

    results_root = Path(sys.argv[1])
    output = Path(sys.argv[2])
    services_out = []
    total = passed = failed = 0

    for key, label, _assembly in SERVICES:
        svc_dir = results_root / key
        trx = find_trx(svc_dir) if svc_dir.exists() else None
        cob = find_cobertura(svc_dir) if svc_dir.exists() else None

        svc_total = svc_passed = svc_failed = 0
        line_cov = branch_cov = 0.0

        if trx:
            svc_total, svc_passed, svc_failed = parse_trx(trx)
        if cob:
            line_cov, branch_cov = parse_cobertura(cob)

        total += svc_total
        passed += svc_passed
        failed += svc_failed

        services_out.append(
            {
                "key": key,
                "name": label,
                "tests": svc_total,
                "passed": svc_passed,
                "failed": svc_failed,
                "lineCoverage": line_cov,
                "branchCoverage": branch_cov,
                "reportPath": f"/coverage/{key}/index.html",
            }
        )

    payload = {
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "backend": {
            "total": total,
            "passed": passed,
            "failed": failed,
            "allPassing": failed == 0 and total > 0,
            "services": services_out,
        },
    }

    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Wrote {output} — {passed}/{total} tests, failed={failed}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
