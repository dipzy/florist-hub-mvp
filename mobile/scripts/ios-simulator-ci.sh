#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/../.." && pwd)"
MOBILE_DIR="$ROOT_DIR/mobile"
IOS_DIR="$MOBILE_DIR/iosApp"
DERIVED="${DERIVED_DATA_PATH:-$MOBILE_DIR/build/DerivedData-ios}"
SCREENSHOT_DIR="${SCREENSHOT_DIR:-$ROOT_DIR/ios-simulator-artifacts/screenshots}"
APP_ARTIFACT_DIR="${APP_ARTIFACT_DIR:-$ROOT_DIR/ios-simulator-artifacts/app}"
SIMULATOR_NAME="${SIMULATOR_NAME:-}"
BUNDLE_ID="${BUNDLE_ID:-com.floristhub.ios}"

mkdir -p "$SCREENSHOT_DIR" "$APP_ARTIFACT_DIR"

echo "==> Testes Kotlin (iosSimulatorArm64 + commonTest)"
cd "$MOBILE_DIR"
./gradlew :shared:iosSimulatorArm64Test :shared:compileKotlinIosSimulatorArm64 --no-daemon -Pkotlin.native.ignoreDisabledTargets=true

echo "==> xcodebuild (simulador, sem assinatura)"
cd "$IOS_DIR"
DEST_UDID="$(SIMULATOR_NAME="$SIMULATOR_NAME" python3 <<'PY'
import json, os, subprocess
name = os.environ.get("SIMULATOR_NAME") or ""
data = json.loads(subprocess.check_output(["xcrun", "simctl", "list", "devices", "available", "-j"], text=True))
candidates = []
for runtime, devices in data.get("devices", {}).items():
    if "iOS" not in runtime:
        continue
    for d in devices:
        if not d.get("isAvailable"):
            continue
        if name and d.get("name") != name:
            continue
        if "iPhone" in d.get("name", ""):
            candidates.append(d)
if not candidates:
    raise SystemExit("Sem simulador iPhone")
candidates.sort(key=lambda d: d["name"], reverse=True)
print(candidates[0]["udid"])
PY
)"
xcodebuild \
  -project iosApp.xcodeproj \
  -scheme iosApp \
  -configuration Debug \
  -sdk iphonesimulator \
  -destination "id=${DEST_UDID}" \
  -derivedDataPath "$DERIVED" \
  ARCHS=arm64 \
  EXCLUDED_ARCHS=x86_64 \
  ONLY_ACTIVE_ARCH=YES \
  CODE_SIGNING_ALLOWED=NO \
  build

APP_PATH="$(find "$DERIVED" -path '*/Build/Products/Debug-iphonesimulator/iosApp.app' -maxdepth 6 | head -1)"
if [[ -z "$APP_PATH" || ! -d "$APP_PATH" ]]; then
  echo "Não foi encontrado iosApp.app em $DERIVED" >&2
  exit 1
fi

echo "==> Empacotar .app do simulador"
ditto -c -k --sequesterRsrc --keepParent "$APP_PATH" "$APP_ARTIFACT_DIR/iosApp-simulator.zip"
cp -R "$APP_PATH" "$APP_ARTIFACT_DIR/iosApp.app"

UDID="$DEST_UDID"

xcrun simctl boot "$UDID" 2>/dev/null || true
xcrun simctl bootstatus "$UDID" -b

xcrun simctl uninstall "$UDID" "$BUNDLE_ID" 2>/dev/null || true
xcrun simctl install "$UDID" "$APP_PATH"

echo "==> Capturas de ecrã (intervalo alinhado ao UiPreviewTour)"
xcrun simctl launch "$UDID" "$BUNDLE_ID" || true

SHOT_INTERVAL=4
SHOT_COUNT=12
for i in $(seq -w 1 "$SHOT_COUNT"); do
  sleep "$SHOT_INTERVAL"
  xcrun simctl io "$UDID" screenshot "$SCREENSHOT_DIR/${i}-simulator.png"
done

xcrun simctl terminate "$UDID" "$BUNDLE_ID" 2>/dev/null || true

echo "Concluído: $APP_PATH"
