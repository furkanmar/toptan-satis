#!/bin/bash
set -e

# ─── rclone config ────────────────────────────────────────────────────────────
mkdir -p /root/.config/rclone
cat > /root/.config/rclone/rclone.conf <<EOF
[r2]
type = s3
provider = Cloudflare
access_key_id = ${R2_ACCESS_KEY_ID}
secret_access_key = ${R2_SECRET_ACCESS_KEY}
endpoint = ${R2_ENDPOINT}
acl = private
no_check_bucket = true
EOF

# ─── cron job (03:00 her gün) ─────────────────────────────────────────────────
mkdir -p /etc/crontabs
echo "0 3 * * * /usr/local/bin/backup.sh >> /var/log/backup.log 2>&1" > /etc/crontabs/root

# Başlangıçta bir kez çalıştır (isteğe bağlı; yorum kaldırılabilir)
# /usr/local/bin/backup.sh

echo "[entrypoint] pg-backup servisi başlatıldı. Cron 03:00'te çalışacak."

# crond -f: foreground, log level 8 (verbose)
exec crond -f
