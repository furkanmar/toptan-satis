#!/bin/bash
set -e

# ─── rclone config ────────────────────────────────────────────────────────────
mkdir -p /root/.config/rclone
cat > /root/.config/rclone/rclone.conf <<RCLONE
[r2]
type = s3
provider = Cloudflare
access_key_id = ${R2_ACCESS_KEY_ID}
secret_access_key = ${R2_SECRET_ACCESS_KEY}
endpoint = ${R2_ENDPOINT}
acl = private
no_check_bucket = true
RCLONE

echo "[entrypoint] pg-backup servisi başlatıldı. Her gün 03:00'te çalışacak."

# ─── 03:00'e kaç saniye kaldığını hesapla ────────────────────────────────────
secs_until_3am() {
    local h m s total target
    h=$(date +%H); m=$(date +%M); s=$(date +%S)
    total=$((10#$h * 3600 + 10#$m * 60 + 10#$s))
    target=$((3 * 3600))   # 03:00:00
    if [ "$total" -lt "$target" ]; then
        echo $((target - total))
    else
        echo $((86400 - total + target))
    fi
}

# ─── Döngü ───────────────────────────────────────────────────────────────────
while true; do
    WAIT=$(secs_until_3am)
    echo "[$(date -u +%FT%TZ)] Sonraki backup: ${WAIT}s sonra (03:00 lokal)"
    sleep "$WAIT"
    /usr/local/bin/backup.sh >> /var/log/backup.log 2>&1
    # Aynı dakikada tekrar tetiklenmesin
    sleep 61
done
