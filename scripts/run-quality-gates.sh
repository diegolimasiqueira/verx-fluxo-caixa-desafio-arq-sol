#!/usr/bin/env bash
# E2E (Playwright) + teste de carga (k6) com stack rodando.
# Gera e2e-results.json e load-results.json em frontend/public/
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
BFF_URL="${BFF_URL:-http://localhost:5000}"
FRONTEND_URL="${E2E_BASE_URL:-http://localhost:3000}"
LOAD_SUMMARY="$ROOT/.k6-out/load-summary-raw.json"
K6_OUT="$ROOT/.k6-out"

echo "==> Aguardando stack ($BFF_URL, $FRONTEND_URL)..."
for i in $(seq 1 60); do
  if curl -sf "$BFF_URL/swagger/index.html" >/dev/null && curl -sf "$FRONTEND_URL" >/dev/null; then
    break
  fi
  if [ "$i" -eq 60 ]; then
    echo "Stack indisponível. Suba com: ./build.sh -d" >&2
    exit 1
  fi
  sleep 2
done

echo "==> Seed de saldo para teste de carga (via API)..."
TOKEN=$(curl -sf -X POST "$BFF_URL/api/auth/login" \
  -H 'Content-Type: application/json' \
  -d '{"email":"admin@admin.com","password":"Master@123"}' | python3 -c "import sys,json; print(json.load(sys.stdin)['accessToken'])")

DATE="${BALANCE_DATE:-2026-06-17}"
curl -sf -X POST "$BFF_URL/api/launches" \
  -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' \
  -d "{\"date\":\"$DATE\",\"amount\":1000,\"type\":\"credit\",\"description\":\"load-test-seed\"}" >/dev/null || true
sleep 5

echo "==> E2E (Playwright)..."
cd "$ROOT/frontend"
npm install --silent
npx playwright install chromium
E2E_BASE_URL="$FRONTEND_URL" npx playwright test || E2E_FAILED=1
python3 "$ROOT/scripts/transform-e2e-results.py" \
  "$ROOT/frontend/e2e-report-raw.json" \
  "$ROOT/frontend/public/e2e-results.json"

echo "==> Teste de carga (k6 — 50 req/s, 30s)..."
run_k6_local() {
  local balance_url="${BALANCE_URL:-$BFF_URL}"
  k6 run "$ROOT/tests/load/balance-consolidated.js" \
    --env "BFF_URL=$BFF_URL" \
    --env "BALANCE_URL=$balance_url" \
    --env "BALANCE_DATE=$DATE" \
    --summary-export="$LOAD_SUMMARY"
}

resolve_compose_network() {
  docker inspect cashflow-bff -f '{{range $k, $v := .NetworkSettings.Networks}}{{$k}}{{end}}' 2>/dev/null | head -1
}

run_k6_docker() {
  local network
  network=$(resolve_compose_network)
  if [ -z "$network" ]; then
    echo "    Rede Docker do compose não encontrada — usando host network" >&2
    docker run --rm --network host \
      -v "$ROOT/tests/load:/scripts:ro" \
      -v "$K6_OUT:/out" \
      grafana/k6:latest run /scripts/balance-consolidated.js \
      --env "BFF_URL=$BFF_URL" \
      --env "BALANCE_URL=${BALANCE_URL:-$BFF_URL}" \
      --env "BALANCE_DATE=$DATE" \
      --summary-export=/out/load-summary-raw.json
    return
  fi

  echo "    Rede Docker: $network — alvo: daily-balance-service:8080 (sem rate limit do BFF)"
  docker run --rm --network "$network" \
    -v "$ROOT/tests/load:/scripts:ro" \
    -v "$K6_OUT:/out" \
    grafana/k6:latest run /scripts/balance-consolidated.js \
    --env "BFF_URL=http://bff:8080" \
    --env "BALANCE_URL=http://daily-balance-service:8080" \
    --env "BALANCE_DATE=$DATE" \
    --summary-export=/out/load-summary-raw.json
}

rm -f "$LOAD_SUMMARY"
mkdir -p "$K6_OUT"
chmod 777 "$K6_OUT"
K6_EXIT=0
set +e
if command -v k6 >/dev/null 2>&1; then
  run_k6_local
  K6_EXIT=$?
elif command -v docker >/dev/null 2>&1; then
  echo "    k6 local ausente — usando imagem grafana/k6 via Docker"
  run_k6_docker
  K6_EXIT=$?
else
  echo "    k6 e Docker indisponíveis — pulando teste de carga" >&2
  K6_EXIT=1
fi
set -e

if [ "$K6_EXIT" -ne 0 ] && [ ! -s "$LOAD_SUMMARY" ]; then
  export K6_EXECUTION_FAILED=1
fi
python3 "$ROOT/scripts/transform-load-results.py" \
  "$LOAD_SUMMARY" \
  "$ROOT/frontend/public/load-results.json"
unset K6_EXECUTION_FAILED

echo "==> Rebuild frontend (dist local + imagem Docker)..."
npm run build

cd "$ROOT"
docker compose build frontend
docker compose up -d frontend

if [ "${E2E_FAILED:-0}" -eq 1 ]; then
  echo "E2E falhou — verifique os logs acima." >&2
  exit 1
fi

echo "==> Concluído. Métricas em frontend/public/{test-metrics,e2e-results,load-results}.json"
echo "    Frontend atualizado em http://localhost:3000/tests"
