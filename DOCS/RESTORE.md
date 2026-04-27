# PostgreSQL Backup Restore Prosedürü

Bu dokümanda `marifoglu-backups` R2 bucket'ından bir yedeği Pi'ye indirip restore etme adımları anlatılmıştır.

---

## Gereksinimler

- Pi'de Docker Compose çalışıyor olmalı
- `rclone` Pi'de kurulu olmalı (veya aşağıdaki `docker run` yöntemi kullanılabilir)
- `.env` dosyasında R2 kimlik bilgileri dolu olmalı

---

## 1. Mevcut yedekleri listele

R2'deki mevcut yedeklere bak:

```bash
# Pi terminalinde
docker run --rm \
  -e RCLONE_CONFIG_R2_TYPE=s3 \
  -e RCLONE_CONFIG_R2_PROVIDER=Cloudflare \
  -e RCLONE_CONFIG_R2_ACCESS_KEY_ID="${R2_ACCESS_KEY_ID}" \
  -e RCLONE_CONFIG_R2_SECRET_ACCESS_KEY="${R2_SECRET_ACCESS_KEY}" \
  -e RCLONE_CONFIG_R2_ENDPOINT="${R2_ENDPOINT}" \
  rclone/rclone ls r2:marifoglu-backups
```

Çıktı örneği:
```
12345678 backup_20260427_030001.dump
11234567 backup_20260428_030001.dump
```

---

## 2. Yedeği indir

```bash
# İndirilecek dosyayı seç (en son yedek önerilir)
BACKUP_FILE="backup_20260428_030001.dump"

docker run --rm \
  -v $(pwd)/restore:/data \
  -e RCLONE_CONFIG_R2_TYPE=s3 \
  -e RCLONE_CONFIG_R2_PROVIDER=Cloudflare \
  -e RCLONE_CONFIG_R2_ACCESS_KEY_ID="${R2_ACCESS_KEY_ID}" \
  -e RCLONE_CONFIG_R2_SECRET_ACCESS_KEY="${R2_SECRET_ACCESS_KEY}" \
  -e RCLONE_CONFIG_R2_ENDPOINT="${R2_ENDPOINT}" \
  rclone/rclone copy "r2:marifoglu-backups/${BACKUP_FILE}" /data/

ls -lh restore/
```

---

## 3. Mevcut container'ı durdur (veri kaybı önlemek için)

```bash
# API durdur (DB'ye yazma dursun)
docker compose stop api
```

---

## 4. Restore et

### 4a. Mevcut DB'yi temizle ve restore et (önerilen)

```bash
# Değişkenleri .env'den yükle
source .env

BACKUP_FILE="backup_20260428_030001.dump"

# pg_restore: önce DB'yi temizle (-c), sonra restore et
docker run --rm \
  -v $(pwd)/restore:/backups \
  --network wholesale_default \
  postgres:16-alpine \
  pg_restore \
    -h postgres \
    -U "${POSTGRES_USER}" \
    -d "${POSTGRES_DB}" \
    -Fc \
    -c \
    --if-exists \
    /backups/${BACKUP_FILE}
```

> **Not:** `-c --if-exists` mevcut tabloları temizler ve yeniden oluşturur.
> Prod'da bunu yapmadan önce mevcut DB'nin yedeğini al.

### 4b. Yeni/boş DB'ye restore (tam sıfırlama)

```bash
# Sadece volume'u sil ve yeniden başlat:
docker compose down -v
docker compose up -d postgres

# Healthy olmasını bekle
docker compose exec postgres pg_isready

# Restore
source .env
docker run --rm \
  -v $(pwd)/restore:/backups \
  --network wholesale_default \
  postgres:16-alpine \
  pg_restore \
    -h postgres \
    -U "${POSTGRES_USER}" \
    -d "${POSTGRES_DB}" \
    -Fc \
    /backups/${BACKUP_FILE}
```

---

## 5. API'yi tekrar başlat

```bash
docker compose start api
# veya tam restart:
docker compose up -d
```

Logları kontrol et:
```bash
docker compose logs api --tail=50 -f
```

---

## 6. Restore sonrası doğrulama

```bash
# Kayıt sayılarını kontrol et
docker compose exec postgres psql \
  -U "${POSTGRES_USER}" \
  -d "${POSTGRES_DB}" \
  -c "SELECT 
        (SELECT count(*) FROM \"Products\") AS products,
        (SELECT count(*) FROM \"Orders\") AS orders,
        (SELECT count(*) FROM \"Users\") AS users;"
```

---

## Staging Ortamında Test Senaryosu

Aşağıdaki adımları prod olmayan bir ortamda (örn. farklı port/compose dosyası) uygulayarak restore'un çalıştığını doğrulayabilirsin:

```bash
# 1. Test ortamı için ayrı compose
COMPOSE_PROJECT_NAME=wholesale_test \
  docker compose -f docker-compose.yml \
  -p wholesale_test up -d postgres

# 2. Restore et (test DB'sine)
source .env
docker run --rm \
  -v $(pwd)/restore:/backups \
  --network wholesale_test_default \
  postgres:16-alpine \
  pg_restore \
    -h postgres \
    -U "${POSTGRES_USER}" \
    -d "${POSTGRES_DB}" \
    -Fc \
    /backups/${BACKUP_FILE}

# 3. Kontrol
docker run --rm \
  --network wholesale_test_default \
  postgres:16-alpine \
  psql -h postgres -U "${POSTGRES_USER}" -d "${POSTGRES_DB}" \
    -c "SELECT count(*) FROM \"Products\";"

# 4. Test ortamını temizle
COMPOSE_PROJECT_NAME=wholesale_test docker compose down -v
```

---

## Backup Zamanlaması

| Olay | Zaman |
|------|-------|
| pg_dump çalışır | Her gün 03:00 (UTC) |
| R2'ye yüklenir | Aynı süreçte |
| Eski yedekler silinir | 7 günden eski → otomatik |
| Hata bildirimi | Telegram (bot: Furkanpibot) |

Backup loglarını görmek için:
```bash
docker compose logs pg-backup --tail=100
```
