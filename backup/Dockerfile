FROM postgres:16-alpine

# rclone + bash + curl + cron
RUN apk add --no-cache rclone bash curl

COPY entrypoint.sh /entrypoint.sh
COPY backup.sh     /usr/local/bin/backup.sh

RUN chmod +x /entrypoint.sh /usr/local/bin/backup.sh

ENTRYPOINT ["/entrypoint.sh"]
