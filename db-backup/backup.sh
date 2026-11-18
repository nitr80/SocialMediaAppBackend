#!/bin/sh
#
# Periodic PostgreSQL backup sidecar.
#
# Runs `pg_dump` (custom format) against the configured database, uploads the
# dump to an S3-compatible bucket (e.g. Cloudflare R2), then deletes dumps older
# than the retention window.
#
# Two modes:
#   - Long-running (default): runs a backup cycle, sleeps, repeats. Used by the
#     `backup` service in compose.yaml.
#   - One-shot (BACKUP_ONCE=true): runs a single cycle and exits, for ad-hoc
#     backups.

set -eu

log() {
    echo "[backup] $(date -u +%Y-%m-%dT%H:%M:%SZ) $*"
}

# --- Required configuration -------------------------------------------------
# When any of these are missing the service idles instead of crash-looping, so
# `docker compose up` works for developers who do not run backups.
missing=""
for name in PGUSER PGPASSWORD PGDATABASE S3_ENDPOINT S3_BUCKET AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY; do
    eval "value=\${$name:-}"
    if [ -z "$value" ]; then
        missing="$missing $name"
    fi
done

if [ -n "$missing" ]; then
    log "backups disabled; missing environment:$missing"
    log "set the BACKUP_* variables (see README) to enable automated backups"
    exec sleep infinity
fi

PGHOST="${PGHOST:-db}"
PGPORT="${PGPORT:-5432}"
S3_REGION="${S3_REGION:-auto}"
S3_PREFIX="${S3_PREFIX:-backups}"
BACKUP_INTERVAL_SECONDS="${BACKUP_INTERVAL_SECONDS:-86400}"
BACKUP_RETENTION_DAYS="${BACKUP_RETENTION_DAYS:-7}"

export PGHOST PGPORT PGUSER PGPASSWORD PGDATABASE

# All S3 calls share the same endpoint/region flags.
s3() {
    aws --endpoint-url "$S3_ENDPOINT" --region "$S3_REGION" "$@"
}

backup_once() {
    timestamp="$(date -u +%Y%m%dT%H%M%SZ)"
    key="${S3_PREFIX}/${PGDATABASE}-${timestamp}.dump"
    file="$(mktemp)"

    log "dumping ${PGDATABASE} from ${PGHOST}:${PGPORT}"
    pg_dump --format=custom --no-owner --no-privileges --file="$file"

    log "uploading s3://${S3_BUCKET}/${key}"
    s3 s3 cp "$file" "s3://${S3_BUCKET}/${key}"

    rm -f "$file"
    log "backup complete: ${key}"
}

prune() {
    cutoff="$(date -u -d "-${BACKUP_RETENTION_DAYS} days" +%Y-%m-%dT%H:%M:%SZ)"
    log "pruning dumps older than ${cutoff} (retention ${BACKUP_RETENTION_DAYS}d)"

    s3 s3api list-objects-v2 \
        --bucket "$S3_BUCKET" \
        --prefix "${S3_PREFIX}/" \
        --query "Contents[?LastModified<='${cutoff}'].Key" \
        --output text \
        | tr '\t' '\n' \
        | while IFS= read -r key; do
            case "$key" in
                ""|"None") continue ;;
            esac
            log "deleting s3://${S3_BUCKET}/${key}"
            s3 s3 rm "s3://${S3_BUCKET}/${key}"
        done
}

if [ "${BACKUP_ONCE:-false}" = "true" ]; then
    log "running one-shot backup"
    backup_once
    prune
    log "done"
    exit 0
fi

trap 'log "shutting down"; exit 0' TERM INT

while true; do
    if backup_once && prune; then
        log "cycle complete; next run in ${BACKUP_INTERVAL_SECONDS}s"
    else
        log "cycle failed; retrying in ${BACKUP_INTERVAL_SECONDS}s"
    fi

    # Backgrounded so a TERM/INT can interrupt the sleep immediately.
    sleep "$BACKUP_INTERVAL_SECONDS" &
    wait $!
done
