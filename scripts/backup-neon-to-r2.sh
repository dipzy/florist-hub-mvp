#!/usr/bin/env bash
# Cópia comprimida do Postgres (Neon ou local) para object storage compatível S3
# (Neon Object Storage, Cloudflare R2, etc.).
# Uso: DATABASE_URL=postgresql://... ./scripts/backup-neon-to-r2.sh [ficheiro.sql.gz]
set -euo pipefail

if [[ -z "${DATABASE_URL:-}" ]]; then
  echo "Defina DATABASE_URL com a ligação directa (no Neon, sem -pooler)." >&2
  exit 1
fi

S3_ENDPOINT="${BACKUP_S3_ENDPOINT:-${NEON_S3_ENDPOINT:-${R2_ENDPOINT:-}}}"
S3_BUCKET="${BACKUP_S3_BUCKET:-${NEON_S3_BUCKET:-${R2_BUCKET:-}}}"
S3_ACCESS_KEY_ID="${BACKUP_S3_ACCESS_KEY_ID:-${NEON_S3_ACCESS_KEY_ID:-${R2_ACCESS_KEY_ID:-}}}"
S3_SECRET_ACCESS_KEY="${BACKUP_S3_SECRET_ACCESS_KEY:-${NEON_S3_SECRET_ACCESS_KEY:-${R2_SECRET_ACCESS_KEY:-}}}"
S3_REGION="${BACKUP_S3_REGION:-${NEON_S3_REGION:-${R2_REGION:-auto}}}"

stamp="$(date -u +%Y%m%dT%H%M%SZ)"
outfile="${1:-/tmp/floristhub-${stamp}.sql.gz}"
mkdir -p "$(dirname "$outfile")"

pg_dump --format=plain --no-owner --no-privileges --dbname="$DATABASE_URL" | gzip -9 > "$outfile"
echo "Cópia local: $outfile ($(wc -c < "$outfile") bytes)"

if [[ -z "$S3_BUCKET" ]]; then
  echo "Bucket S3 vazio (BACKUP_S3_BUCKET / NEON_S3_BUCKET / R2_BUCKET): a cópia ficou só no disco."
  exit 0
fi

: "${S3_ENDPOINT:?Defina BACKUP_S3_ENDPOINT (ou NEON_S3_ENDPOINT / R2_ENDPOINT)}"
: "${S3_ACCESS_KEY_ID:?Defina BACKUP_S3_ACCESS_KEY_ID (ou equivalente Neon/R2)}"
: "${S3_SECRET_ACCESS_KEY:?Defina BACKUP_S3_SECRET_ACCESS_KEY (ou equivalente Neon/R2)}"

if ! command -v aws >/dev/null 2>&1; then
  echo "O comando aws não está instalado. A cópia local foi criada; instale a AWS CLI para enviar ao bucket." >&2
  exit 1
fi

prefix="${BACKUP_S3_PREFIX:-${R2_PREFIX:-backups/floristhub}}"
retention="${BACKUP_RETENTION_DAYS:-14}"
key="${prefix}/floristhub-${stamp}.sql.gz"

export AWS_ACCESS_KEY_ID="$S3_ACCESS_KEY_ID"
export AWS_SECRET_ACCESS_KEY="$S3_SECRET_ACCESS_KEY"
export AWS_DEFAULT_REGION="$S3_REGION"

aws s3 cp "$outfile" "s3://${S3_BUCKET}/${key}" --endpoint-url "$S3_ENDPOINT" --only-show-errors
echo "Enviado para s3://${S3_BUCKET}/${key}"

cutoff="$(date -u -d "-${retention} days" +%Y-%m-%d)"
aws s3 ls "s3://${S3_BUCKET}/${prefix}/" --endpoint-url "$S3_ENDPOINT" --recursive | while read -r date time size object; do
  [[ -z "${object:-}" ]] && continue
  if [[ "$date" < "$cutoff" ]]; then
    aws s3 rm "s3://${S3_BUCKET}/${object}" --endpoint-url "$S3_ENDPOINT" --only-show-errors
    echo "Removido (mais antigo que ${retention} dias): ${object}"
  fi
done
