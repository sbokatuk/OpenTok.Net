#!/usr/bin/env bash
set -euo pipefail

# Builds the device test app against the packed OpenTok.Net packages, installs it on a running
# Android emulator and runs its checks. The app reports its verdict to logcat under a single tag;
# this script turns that into an exit code.
#
# Assumes an emulator is already booted and visible to adb - in CI that is
# reactivecircus/android-emulator-runner, locally it is whatever you started yourself.
#
# Usage: run-emulator-tests.sh VERSION [TARGET_FRAMEWORK]

VERSION="${1:?a package version is required}"
TARGET_FRAMEWORK="${2:-net10.0-android36.0}"

PACKAGE_NAME="com.sbokatuk.opentoknet.devicetests"
LOG_FILE="emulator-tests.log"
LOG_TAG="OpenTokNetE2E"
# CI emulators are x86_64. Override for a local arm64 emulator on Apple silicon.
DEVICE_RID="${OPENTOK_DEVICE_RID:-android-x64}"
POLL_ATTEMPTS=90
POLL_INTERVAL=5

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PROJECT="${REPO_ROOT}/tests/OpenTok.Net.DeviceTests/OpenTok.Net.DeviceTests.csproj"

# The SDK band is chosen by the *Android API level* in the target framework, not by the .NET
# version alone, because that is what decides which workload owns the runtime packs:
#
#   net8.0-android34.0  -> android 34.0.x, in the .NET 8 band
#   net9.0-android35.0  -> android 35.0.x, in the .NET 9 band
#   net10.0-android36.0 -> android 36.0.x, in the .NET 10 band
#
# The .NET 9 band compiles a net8 app happily - it has the API 34 *reference* packs - and then
# fails at packaging time, because it has no API 34 *runtime* packs and they cannot be restored
# from NuGet:
#
#     error NETSDK1112: The runtime pack for Microsoft.Android.Runtime.34.android-x64 was not
#     downloaded. Try running a NuGet restore with the RuntimeIdentifier 'android-x64'.
case "${TARGET_FRAMEWORK}" in
    net10.0-*) sdk_major=10 ;;
    net8.0-*)  sdk_major=8 ;;
    *)         sdk_major=9 ;;
esac

sdk_version="$(dotnet --list-sdks | grep "^${sdk_major}\." | tail -1 | cut -d' ' -f1)"
if [ -z "${sdk_version}" ]; then
    echo "::error::no .NET ${sdk_major} SDK installed, cannot build ${TARGET_FRAMEWORK}"
    exit 1
fi

SDK_DIR="$(mktemp -d)"
trap 'rm -rf "${SDK_DIR}"' EXIT
printf '{ "sdk": { "version": "%s", "rollForward": "latestFeature" } }\n' "${sdk_version}" \
    > "${SDK_DIR}/global.json"

# NuGet caches by package id + version, so rebuilding a version that was already restored once
# silently reuses the stale copy. Read from packages.tsv so a package added to the build is
# cleared here too.
while IFS=$'\t' read -r id _rest; do
    case "${id}" in ''|\#*) continue ;; esac
    lower="$(printf '%s' "${id}" | tr '[:upper:]' '[:lower:]')"
    rm -rf "${HOME}/.nuget/packages/${lower}/${VERSION}"
done < "${REPO_ROOT}/build/packages.tsv"

rm -rf "${REPO_ROOT}/tests/OpenTok.Net.DeviceTests/obj" \
       "${REPO_ROOT}/tests/OpenTok.Net.DeviceTests/bin"

echo "==> building device tests (version=${VERSION}, tfm=${TARGET_FRAMEWORK}, sdk=${sdk_version})"
# Debug, not Release. Release AOT-compiles every assembly, and this suite only needs to verify
# that the package carries its .aar files and that the native SDK loads and responds through the
# binding - nothing AOT affects. The sample job covers the Release build path.
( cd "${SDK_DIR}" && dotnet build "${PROJECT}" \
    --configuration Debug \
    -p:OpenTokPackageVersion="${VERSION}" \
    -p:OpenTokDeviceTargetFramework="${TARGET_FRAMEWORK}" \
    -p:RuntimeIdentifier="${DEVICE_RID}" \
    -t:Install )

echo "==> launching"
adb logcat -c
# The activity name is pinned in the app rather than left to the generated crc64* name, so this
# target stays stable across builds.
adb shell am start -n "${PACKAGE_NAME}/.MainActivity"

echo "==> waiting for the verdict"
for _ in $(seq "${POLL_ATTEMPTS}"); do
    if adb logcat -d -s "${LOG_TAG}:*" | grep -q "OPENTOK_E2E_DONE"; then
        break
    fi
    sleep "${POLL_INTERVAL}"
done

adb logcat -d -s "${LOG_TAG}:*" | tee "${LOG_FILE}"

if ! grep -q "OPENTOK_E2E_DONE PASS" "${LOG_FILE}"; then
    # No verdict usually means the app died before reporting, so keep the crash trace. A missing
    # Java dependency shows up here as a NoClassDefFoundError naming the class.
    echo "==> no passing verdict; capturing crash output"
    adb logcat -d -s AndroidRuntime:E DEBUG:F "${PACKAGE_NAME}:*" 2>/dev/null \
        | tail -100 | tee -a "${LOG_FILE}" || true
    echo "::error::OpenTok emulator checks failed or timed out"
    exit 1
fi

echo "==> emulator checks passed"
