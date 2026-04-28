#!/bin/bash
set -euo pipefail

# ─── Config ───────────────────────────────────────────────────────────────────
POSTGRES_HOST="${POSTGRES_HOST:-postgres}"
POSTGRES_USER="${POSTGRES_USER:-wholesale}"
POSTGRES_DB="${POSTGRES_DB:-wholesaledb}"
BACKUP_DIR="/backups"
RETENTION_DAYS="${RETENTION_DAYS:-7}"
R2_BACKUP_BUCKET="${R2_BACKUP_BUCKET:-marifoglu-all}"

DATE=$(date +%Y%m%d_%H%M%S)
BACKUP_FILE="${BACKUP_DIR}/backup_${DATE}.dump"

# ─── Telegram bildirim helper ─────────────────────────────────────────────────
notify() {
    local msg="$1"
    if [[ -n "${TELEGRAM_BOT_TOKEN:-}" && -n "${TELEGRAM_CHAT_ID:-}" ]]; then
        curl -s -X POST "https://api.telegram.org/bot${TELEGRAM_BOT_TOKEN}/sendMessage" \
            --data-urlencode "chat_id=${TELEGRAM_CHAT_ID}" \
            --data-urlencode "text=${msg}" \
            -o /dev/null || true   # bildirim hatası backup'ı durdurmasın
    fi
}

# ─── Backup dizini oluştur ────────────────────────────────────────────────────
mkdir -p "${BACKUP_DIR}"

echo "[$(date -u +%FT%TZ)] ▶ Backup başlıyor: ${BACKUP_FILE}"

# ─── pg_dump ──────────────────────────────────────────────────────────────────
if ! PGPASSWORD="${POSTGRES_PASSWORD}" pg_dump \
        -h "${POSTGRES_HOST}" \
        -U "${POSTGRES_USER}" \
        -d "${POSTGRES_DB}" \
        -Fc \
        -f "${BACKUP_FILE}"; then

    echo "[$(date -u +%FT%TZ)] ✗ pg_dump BAŞARISIZ"
    notify "❌ [marifoglu] pg_dump başarısız oldu! Host: ${POSTGRES_HOST} DB: ${POSTGRES_DB} Zaman: $(date -u +%FT%TZ)"
    exit 1
fi

BACKUP_SIZE=$(du -sh "${BACKUP_FILE}" | cut -f1)
echo "[$(date -u +%FT%TZ)] ✓ pg_dump OK — ${BACKUP_FILE} (${BACKUP_SIZE})"

# ─── R2'ye yükle ──────────────────────────────────────────────────────────────
if ! rclone copy "${BACKUP_FILE}" "r2:${R2_BACKUP_BUCKET}/"; then
    echo "[$(date -u +%FT%TZ)] ✗ R2 upload BAŞARISIZ"
    notify "❌ [marifoglu] R2 backup upload başarısız! Bucket: ${R2_BACKUP_BUCKET} Zaman: $(date -u +%FT%TZ)"
    exit 1
fi

echo "[$(date -u +%FT%TZ)] ✓ R2 upload OK — r2:${R2_BACKUP_BUCKET}/$(basename ${BACKUP_FILE})"

# ─── Local eski yedekleri sil ─────────────────────────────────────────────────
find "${BACKUP_DIR}" -name "backup_*.dump" -mtime +${RETENTION_DAYS} -delete
echo "[$(date -u +%FT%TZ)] ✓ Local temizlik OK (>${RETENTION_DAYS} gün)"

# ─── R2 eski yedekleri sil ───────────────────────────────────────────────────
rclone delete "r2:${R2_BACKUP_BUCKET}" --min-age "${RETENTION_DAYS}d"
echo "[$(date -u +%FT%TZ)] ✓ R2 temizlik OK (>${RETENTION_DAYS} gün)"

echo "[$(date -u +%FT%TZ)] ✅ Backup tamamlandı — ${BACKUP_FILE} (${BACKUP_SIZE})"
