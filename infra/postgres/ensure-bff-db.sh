#!/usr/bin/env bash
# Cria o banco bff_db em volumes Postgres já existentes (init.sql só roda na 1ª subida).
set -e

CONTAINER="${1:-cashflow-postgres}"

exists=$(docker exec "$CONTAINER" psql -U postgres -tAc \
  "SELECT 1 FROM pg_database WHERE datname = 'bff_db'")

if [ "$exists" != "1" ]; then
  docker exec "$CONTAINER" psql -U postgres -c "CREATE DATABASE bff_db OWNER cashflow;"
  echo "bff_db criado"
else
  echo "bff_db já existe"
fi

docker exec "$CONTAINER" psql -U postgres -c "GRANT ALL PRIVILEGES ON DATABASE bff_db TO cashflow;"

echo "bff_db OK"
