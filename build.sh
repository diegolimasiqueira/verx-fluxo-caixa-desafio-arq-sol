#!/usr/bin/env bash
# Build e inicialização local do ambiente completo.
# Publica os serviços .NET, roda testes com cobertura, gera relatórios HTML
# e compila o frontend antes de subir o docker compose.
set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export PATH="$PATH:$HOME/.dotnet/tools"

latest_cobertura() {
  find "$1" -name 'coverage.cobertura.xml' ! -path '*_fedora_*' -printf '%T@ %p\n' 2>/dev/null \
    | sort -n | tail -1 | cut -d' ' -f2-
}

echo "==> [1/8] Publishing BFF..."
mkdir -p "$ROOT/build-output/bff"
dotnet publish "$ROOT/src/Bff/CashFlow.Bff.Api" \
  -c Release -o "$ROOT/build-output/bff" --nologo --verbosity minimal

echo "==> [2/8] Publishing LaunchService..."
mkdir -p "$ROOT/build-output/launch"
dotnet publish "$ROOT/src/LaunchService/CashFlow.LaunchService.Api" \
  -c Release -o "$ROOT/build-output/launch" --nologo --verbosity minimal

echo "==> [3/8] Publishing DailyBalanceService..."
mkdir -p "$ROOT/build-output/balance"
dotnet publish "$ROOT/src/DailyBalanceService/CashFlow.DailyBalanceService.Api" \
  -c Release -o "$ROOT/build-output/balance" --nologo --verbosity minimal

echo "==> [4/8] Publishing DailyBalanceWorker..."
mkdir -p "$ROOT/build-output/worker"
dotnet publish "$ROOT/src/DailyBalanceWorker/CashFlow.DailyBalanceWorker" \
  -c Release -o "$ROOT/build-output/worker" --nologo --verbosity minimal

echo "==> [5/8] Running tests with coverage..."
rm -rf "$ROOT/coverage-results"

# Pre-criar dirs que evitam bug do SDK na build Debug de projetos WebApi
mkdir -p "$ROOT/src/LaunchService/CashFlow.LaunchService.Api/obj/Debug/net10.0/staticwebassets"
mkdir -p "$ROOT/src/DailyBalanceService/CashFlow.DailyBalanceService.Api/obj/Debug/net10.0/staticwebassets"

mkdir -p "$ROOT/src/Bff/CashFlow.Bff.Api/obj/Debug/net10.0/staticwebassets"

dotnet test "$ROOT/src/Bff/CashFlow.Bff.Api.Tests" \
  --settings "$ROOT/coverlet.runsettings" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$ROOT/coverage-results/bff" \
  --logger "trx;LogFileName=bff-results.trx" \
  --nologo --verbosity minimal

dotnet test "$ROOT/src/LaunchService/CashFlow.LaunchService.Tests" \
  --settings "$ROOT/coverlet.runsettings" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$ROOT/coverage-results/launch" \
  --logger "trx;LogFileName=launch-results.trx" \
  --nologo --verbosity minimal

dotnet test "$ROOT/src/DailyBalanceService/CashFlow.DailyBalanceService.Tests" \
  --settings "$ROOT/coverlet.runsettings" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$ROOT/coverage-results/balance" \
  --logger "trx;LogFileName=balance-results.trx" \
  --nologo --verbosity minimal

dotnet test "$ROOT/src/DailyBalanceWorker/CashFlow.DailyBalanceWorker.Tests" \
  --settings "$ROOT/coverlet.runsettings" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$ROOT/coverage-results/worker" \
  --logger "trx;LogFileName=worker-results.trx" \
  --nologo --verbosity minimal

find "$ROOT/coverage-results" -path '*_fedora_*' -name 'coverage.cobertura.xml' -delete 2>/dev/null || true

python3 "$ROOT/scripts/generate-test-metrics.py" \
  "$ROOT/coverage-results" \
  "$ROOT/frontend/public/test-metrics.json"

python3 "$ROOT/scripts/transform-e2e-results.py" \
  "$ROOT/frontend/e2e-report-raw.json" \
  "$ROOT/frontend/public/e2e-results.json"
python3 "$ROOT/scripts/transform-load-results.py" \
  "$ROOT/load-summary-raw.json" \
  "$ROOT/frontend/public/load-results.json"

echo "==> [6/8] Generating HTML coverage reports..."
rm -rf "$ROOT/frontend/public/coverage/bff" \
       "$ROOT/frontend/public/coverage/launch" \
       "$ROOT/frontend/public/coverage/balance" \
       "$ROOT/frontend/public/coverage/worker"
mkdir -p "$ROOT/frontend/public/coverage/launch" \
         "$ROOT/frontend/public/coverage/balance" \
         "$ROOT/frontend/public/coverage/worker" \
         "$ROOT/frontend/public/coverage/bff"

reportgenerator \
  -reports:"$(latest_cobertura "$ROOT/coverage-results/bff")" \
  -targetdir:"$ROOT/frontend/public/coverage/bff" \
  -reporttypes:Html \
  -title:"BFF — Cobertura de Testes" \
  -verbosity:Warning

reportgenerator \
  -reports:"$(latest_cobertura "$ROOT/coverage-results/launch")" \
  -targetdir:"$ROOT/frontend/public/coverage/launch" \
  -reporttypes:Html \
  -title:"LaunchService — Cobertura de Testes" \
  -verbosity:Warning

reportgenerator \
  -reports:"$(latest_cobertura "$ROOT/coverage-results/balance")" \
  -targetdir:"$ROOT/frontend/public/coverage/balance" \
  -reporttypes:Html \
  -title:"DailyBalanceService — Cobertura de Testes" \
  -verbosity:Warning

reportgenerator \
  -reports:"$(latest_cobertura "$ROOT/coverage-results/worker")" \
  -targetdir:"$ROOT/frontend/public/coverage/worker" \
  -reporttypes:Html \
  -title:"DailyBalanceWorker — Cobertura de Testes" \
  -verbosity:Warning

echo "==> [7/8] Building Frontend (React)..."
cd "$ROOT/frontend"
npm install --silent
npm run build
cd "$ROOT"

echo "==> [8/8] Starting docker compose..."
if docker ps --format '{{.Names}}' | grep -qx 'cashflow-postgres'; then
  echo "    Ensuring bff_db exists (upgrade de volume existente)..."
  "$ROOT/infra/postgres/ensure-bff-db.sh" || true
fi
docker compose -f "$ROOT/docker-compose.yml" up --build "$@"

echo ""
echo "==> Quality gates (E2E + carga 50 req/s):"
echo "    chmod +x scripts/run-quality-gates.sh && ./scripts/run-quality-gates.sh"
echo "    docker compose restart frontend"
