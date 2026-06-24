#!/usr/bin/env python3
"""Transforma summary export do k6 em formato simplificado para o frontend."""
from __future__ import annotations

import json
import sys
from datetime import datetime, timezone
from pathlib import Path

TARGET_RPS = 50
MAX_ERROR_RATE = 0.05
MIN_ACHIEVED_RPS = TARGET_RPS * 0.95


def metric_value(metrics: dict, name: str, field: str = "value") -> float:
    m = metrics.get(name, {})
    if field in m:
        return float(m[field])
    return float(m.get("values", {}).get(field, 0))


def main() -> int:
    if len(sys.argv) != 3:
        print("Usage: transform-load-results.py <k6-summary.json> <output.json>", file=sys.stderr)
        return 1

    src = Path(sys.argv[1])
    dst = Path(sys.argv[2])

    if not src.is_file() or src.stat().st_size == 0:
        import os
        failed = os.environ.get("K6_EXECUTION_FAILED") == "1"
        dst.write_text(
            json.dumps(
                {
                    "status": "failed" if failed else "pending",
                    "message": (
                        "Execução do k6 falhou — verifique logs do run-quality-gates.sh."
                        if failed
                        else "Execute ./scripts/run-quality-gates.sh com a stack rodando."
                    ),
                    "generatedAt": None,
                    "targetRps": TARGET_RPS,
                    "achievedRps": 0,
                    "durationSeconds": 0,
                    "totalRequests": 0,
                    "errorRate": 0,
                    "p95LatencyMs": 0,
                    "thresholds": {
                        "maxErrorRate": MAX_ERROR_RATE,
                        "errorRatePassed": False,
                        "minAchievedRps": MIN_ACHIEVED_RPS,
                        "targetRpsPassed": False,
                    },
                },
                indent=2,
                ensure_ascii=False,
            )
            + "\n",
            encoding="utf-8",
        )
        return 0

    data = json.loads(src.read_text(encoding="utf-8"))
    metrics = data.get("metrics", {})

    duration_s = metric_value(metrics, "iteration_duration", "max") / 1_000_000_000
    if duration_s <= 0:
        duration_s = metric_value(metrics, "vus", "max")  # fallback

    test_duration = 30.0
    state = data.get("state", {})
    if state.get("testRunDurationMs"):
        test_duration = state["testRunDurationMs"] / 1000

    total_reqs = metric_value(metrics, "http_reqs", "count")
    achieved_rps = round(metric_value(metrics, "http_reqs", "rate"), 1)
    if achieved_rps <= 0 and test_duration:
        achieved_rps = round(total_reqs / test_duration, 1)

    failed_rate = metric_value(metrics, "http_req_failed", "rate")
    p95 = metric_value(metrics, "http_req_duration", "p(95)")

    error_passed = failed_rate <= MAX_ERROR_RATE
    rps_passed = achieved_rps >= MIN_ACHIEVED_RPS
    status = "passed" if error_passed and rps_passed else "failed"

    payload = {
        "status": status,
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "targetRps": TARGET_RPS,
        "achievedRps": achieved_rps,
        "durationSeconds": round(test_duration),
        "totalRequests": int(total_reqs),
        "errorRate": round(failed_rate, 4),
        "p95LatencyMs": round(p95),
        "endpoint": "GET /api/balance/{date} (Daily Balance Service — JWT via BFF login)",
        "thresholds": {
            "maxErrorRate": MAX_ERROR_RATE,
            "errorRatePassed": error_passed,
            "minAchievedRps": MIN_ACHIEVED_RPS,
            "targetRpsPassed": rps_passed,
        },
        "notes": [
            "NFR: 50 req/s no consolidado diário, máx. 5% perda (Documentos/01-requisitos.md).",
            "Cache IMemoryCache (TTL 5 min) no Daily Balance Service.",
            "Artefatos K8s: infra/k8s/ (HPA + KEDA — evolução produção).",
        ],
    }

    dst.parent.mkdir(parents=True, exist_ok=True)
    dst.write_text(json.dumps(payload, indent=2, ensure_ascii=False) + "\n", encoding="utf-8")
    print(f"Wrote {dst} — {achieved_rps} req/s, error={failed_rate:.2%}, status={status}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
