#!/usr/bin/env bash
# Build e inicialização local do ambiente completo.
# Publica os serviços .NET, roda testes com cobertura, gera relatórios HTML
# e compila o frontend antes de subir o docker compose.
set -e

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export PATH="$PATH:$HOME/.dotnet/tools"

echo "==> [1/7] Publishing LaunchService..."
mkdir -p "$ROOT/build-output/launch"
dotnet publish "$ROOT/src/LaunchService/CashFlow.LaunchService.Api" \
  -c Release -o "$ROOT/build-output/launch" --nologo --verbosity minimal

echo "==> [2/7] Publishing DailyBalanceService..."
mkdir -p "$ROOT/build-output/balance"
dotnet publish "$ROOT/src/DailyBalanceService/CashFlow.DailyBalanceService.Api" \
  -c Release -o "$ROOT/build-output/balance" --nologo --verbosity minimal

echo "==> [3/7] Publishing DailyBalanceWorker..."
mkdir -p "$ROOT/build-output/worker"
dotnet publish "$ROOT/src/DailyBalanceWorker/CashFlow.DailyBalanceWorker" \
  -c Release -o "$ROOT/build-output/worker" --nologo --verbosity minimal

echo "==> [4/7] Running tests with coverage..."
rm -rf "$ROOT/coverage-results"

# Pre-criar dirs que evitam bug do SDK na build Debug de projetos WebApi
mkdir -p "$ROOT/src/LaunchService/CashFlow.LaunchService.Api/obj/Debug/net10.0/staticwebassets"
mkdir -p "$ROOT/src/DailyBalanceService/CashFlow.DailyBalanceService.Api/obj/Debug/net10.0/staticwebassets"

dotnet test "$ROOT/src/LaunchService/CashFlow.LaunchService.Tests" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$ROOT/coverage-results/launch" \
  --nologo --verbosity minimal

dotnet test "$ROOT/src/DailyBalanceService/CashFlow.DailyBalanceService.Tests" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$ROOT/coverage-results/balance" \
  --nologo --verbosity minimal

dotnet test "$ROOT/src/DailyBalanceWorker/CashFlow.DailyBalanceWorker.Tests" \
  --collect:"XPlat Code Coverage" \
  --results-directory "$ROOT/coverage-results/worker" \
  --nologo --verbosity minimal

echo "==> [5/7] Generating HTML coverage reports..."
mkdir -p "$ROOT/frontend/public/coverage/launch" \
         "$ROOT/frontend/public/coverage/balance" \
         "$ROOT/frontend/public/coverage/worker"

reportgenerator \
  -reports:"$ROOT/coverage-results/launch/**/coverage.cobertura.xml" \
  -targetdir:"$ROOT/frontend/public/coverage/launch" \
  -reporttypes:Html \
  -title:"LaunchService — Cobertura de Testes" \
  -verbosity:Warning

reportgenerator \
  -reports:"$ROOT/coverage-results/balance/**/coverage.cobertura.xml" \
  -targetdir:"$ROOT/frontend/public/coverage/balance" \
  -reporttypes:Html \
  -title:"DailyBalanceService — Cobertura de Testes" \
  -verbosity:Warning

reportgenerator \
  -reports:"$ROOT/coverage-results/worker/**/coverage.cobertura.xml" \
  -targetdir:"$ROOT/frontend/public/coverage/worker" \
  -reporttypes:Html \
  -title:"DailyBalanceWorker — Cobertura de Testes" \
  -verbosity:Warning

echo "==> [6/7] Building Frontend (React)..."
cd "$ROOT/frontend"
npm install --silent
npm run build
cd "$ROOT"

echo "==> [7/7] Starting docker compose..."
docker compose -f "$ROOT/docker-compose.yml" up --build "$@"
