# Marifoglu Store — Flutter Setup

## 1. Flutter projesi oluştur

```bash
flutter create marifoglu_store
cd marifoglu_store
```

## 2. lib/ klasörünü bu repo'dan kopyala

```bash
# Repo'daki mobile/ klasörünü kullan
cp -r /path/to/toptan-satis/mobile/lib/* lib/
cp /path/to/toptan-satis/mobile/pubspec.yaml .
cp /path/to/toptan-satis/mobile/analysis_options.yaml .
cp /path/to/toptan-satis/mobile/build_apk.sh .
chmod +x build_apk.sh
```

## 3. assets klasörü

```bash
mkdir -p assets/images
```

## 4. Android manifest — kamera izni

`android/app/src/main/AndroidManifest.xml` içine `<application>` tagından ÖNCE:

```xml
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.INTERNET" />
```

## 5. Bağımlılıkları yükle

```bash
flutter pub get
```

## 6. Development'ta çalıştır

```bash
# Android emülatörde (localhost → 10.0.2.2)
flutter run --dart-define=BASE_URL=http://10.0.2.2:5000

# Gerçek cihazda (backend IP'ni yaz)
flutter run --dart-define=BASE_URL=http://192.168.1.X:5000
```

## 7. Production APK

```bash
./build_apk.sh
```

Çıktı: `build/marifoglu-store-v1.0.0-<build>.apk`

---

## Endpoint özeti (backend `api.marifoglu.trade`)

| Endpoint | Kullanım |
|----------|----------|
| `POST /api/auth/login` | JWT alır |
| `GET /api/store-wholesalers/my` | Bağlı toptancılar |
| `GET /api/products?wholesalerId=` | Tüm ürünler (client-side filtre) |
| `POST /api/orders` | Sipariş oluştur (Idempotency-Key otomatik) |
| `GET /api/orders` | Sipariş geçmişi |
| `GET /api/orders/{id}` | Sipariş detayı |
| `GET /api/credit/summary` | Cari özet |
| `GET /api/credit/statement?from=&to=` | Ekstre |
| `GET /api/delivery-notes/{id}/pdf` | İrsaliye PDF |
| `POST /api/users/me/telegram-chat-id` | Telegram bağlantısı |

## Notlar

- **Pagination yok** — tüm ürünler tek seferde yüklenir, client-side filtre yapılır.
  Büyük katalog varsa backend'e `page/pageSize` eklenebilir (Faz 8).
- **Barkod arama client-side** — `ProductUnitConfig.Barcodes` array'inden aranır.
- **Sepet persist** — `flutter_secure_storage`'da, toptancı başına ayrı key.
- **401 → otomatik logout** — `ErrorInterceptor` yakalar, `AuthNotifier.logout()` çağırır.
- **Idempotency-Key** — `POST/PUT/PATCH` isteklere `IdempotencyInterceptor` otomatik ekler.
