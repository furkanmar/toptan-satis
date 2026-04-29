#!/bin/bash
set -e

# Versiyon otomatik artır
VERSION_FILE="pubspec.yaml"
CURRENT=$(grep '^version:' $VERSION_FILE | sed 's/version: //')
BASE=$(echo $CURRENT | cut -d'+' -f1)
BUILD=$(echo $CURRENT | cut -d'+' -f2)
NEW_BUILD=$((BUILD + 1))
NEW_VERSION="${BASE}+${NEW_BUILD}"

echo "📦 Versiyon: $CURRENT → $NEW_VERSION"
sed -i "s/^version: .*/version: $NEW_VERSION/" $VERSION_FILE

# Production APK build
echo "🔨 APK build başlıyor..."
flutter build apk --release \
  --dart-define=BASE_URL=https://api.marifoglu.trade

APK_PATH="build/app/outputs/flutter-apk/app-release.apk"
OUTPUT="build/marifoglu-store-v${BASE}-${NEW_BUILD}.apk"
cp $APK_PATH $OUTPUT

echo "✅ APK hazır: $OUTPUT"
echo "📤 GitHub Releases'a yükle veya mağazalara ilet."
