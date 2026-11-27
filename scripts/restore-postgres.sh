#!/usr/bin/env bash
# Restaura um .sql.gz criado por backup-neon-to-r2.sh.
# Por omissão só aceita destinos locais. Para um Neon real: RESTORE_I_UNDERSTAND=yes
# Uso: DATABASE_URL=postgresql://... ./scripts/restore-postgres.sh /caminho/floristhub.sql.gz
set -euo pipefail

file="${1:?Indique o ficheiro .sql.gz}"
if [[ ! -f "$file" ]]; then
  echo "Ficheiro em falta: $file" >&2
  exit 1
fi
if [[ -z "${DATABASE_URL:-}" ]]; then
  echo "Defina DATABASE_URL com o destino do restauro." >&2
  exit 1
fi

local_target=0
if [[ "$DATABASE_URL" == *"@localhost"* || "$DATABASE_URL" == *"@127.0.0.1"* || "$DATABASE_URL" == *"Host=localhost"* || "$DATABASE_URL" == *"Host=127.0.0.1"* ]]; then
  local_target=1
fi
if [[ "$local_target" -ne 1 && "${RESTORE_I_UNDERSTAND:-}" != "yes" ]]; then
  echo "Restauro recusado: o destino não parece local. Para restaurar um servidor remoto, RESTORE_I_UNDERSTAND=yes." >&2
  exit 1
fi

echo "A restaurar $file"
gunzip -c "$file" | psql "$DATABASE_URL" --set ON_ERROR_STOP=1 --quiet
echo "Restauro concluído."
