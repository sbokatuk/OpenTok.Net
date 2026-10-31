#!/usr/bin/env bash
set -euo pipefail

# Builds the device test app against the packed OpenTok.Net packages, installs it on an iOS
# simulator and runs its checks. The app prints its verdict to stdout; this script turns that into
# an exit code.
#
# Usage: run-simulator-tests.sh VERSION [TARGET_FRAMEWORK]

VERSION="${1:?a package version is required}"
TARGET_FRAMEWORK="${2:-net10.0-ios26.0}"

BUNDLE_ID="com.sbokatuk.opentoknet.devicetests"
# The name the simulator's unified log files the app's lines under.
APP_PROCESS="OpenTok.Net.DeviceTests"
LOG_FILE="simulator-tests.log"
# CI runners are Apple silicon. Override for an Intel runner, whose simulator is x64.
SIMULATOR_RID="${OPENTOK_SIMULATOR_RID:-iossimulator-arm64}"
SIMULATOR_DEVICE="${OPENTOK_SIMULATOR_DEVICE:-iPhone 17}"

REPO_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
PROJECT="${REPO_ROOT}/tests/OpenTok.Net.DeviceTests/OpenTok.Net.DeviceTests.csproj"

# The .NET 9 band builds net8/net9 and the .NET 10 band builds net10, so pick the SDK that owns the
# requested target framework. The SDK is resolved from the working directory, and the repository's
# global.json pins .NET 9, hence the scratch directory - the same pattern build/BuildNugets.sh uses
# for its own second pack pass.
case "${TARGET_FRAMEWORK}" in
    net10.0-*) sdk_major=10 ;;
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
# silently reuses the stale copy. CI versions are unique, but locally you will re-pack the same
# version repeatedly and test yesterday's bits without this. Read from packages.tsv so a package
# added to the build is cleared here too.
while IFS=$'\t' read -r id _rest; do
    case "${id}" in ''|\#*) continue ;; esac
    lower="$(printf '%s' "${id}" | tr '[:upper:]' '[:lower:]')"
    rm -rf "${HOME}/.nuget/packages/${lower}/${VERSION}"
done < "${REPO_ROOT}/build/packages.tsv"

# The app's own intermediate output has to go too, not just the NuGet cache: the OpenTok
# xcframework is extracted out of the package into obj/ and copied into the .app, and neither step
# re-runs when the package version string is unchanged - so a rebuilt package of the same version
# leaves the *previous* build's xcframework embedded in the app.
rm -rf "${REPO_ROOT}/tests/OpenTok.Net.DeviceTests/obj" \
       "${REPO_ROOT}/tests/OpenTok.Net.DeviceTests/bin"

echo "==> building device tests (version=${VERSION}, tfm=${TARGET_FRAMEWORK}, sdk=${sdk_version})"
# Debug, not Release - a Release build AOT-compiles every assembly in the app, and this suite
# verifies that the package carries its xcframework, that the native SDK loads, and that the
# binding drives it correctly, none of which AOT affects. The sample job builds a real MAUI app
# against the same packages in Release and covers that path.
( cd "${SDK_DIR}" && dotnet build "${PROJECT}" \
    --configuration Debug \
    -p:OpenTokPackageVersion="${VERSION}" \
    -p:OpenTokDeviceTargetFramework="${TARGET_FRAMEWORK}" \
    -p:RuntimeIdentifier="${SIMULATOR_RID}" )

APP_PATH="$(find "${REPO_ROOT}/tests/OpenTok.Net.DeviceTests/bin/Debug/${TARGET_FRAMEWORK}/${SIMULATOR_RID}" \
    -maxdepth 1 -name '*.app' -print -quit)"
if [ -z "${APP_PATH}" ]; then
    echo "::error::no .app bundle was produced"
    exit 1
fi

echo "==> selecting simulator"
# Prefer the requested device, but fall back to any available iPhone rather than failing: which
# device names exist depends on the installed Xcode, and pinning one couples this script to a
# runner image that changes without notice. Newest runtime first, so the picked device is the most
# current one available.
#
# The device name goes to python through the environment rather than being interpolated into the
# script text: macOS still ships bash 3.2, so the ${VAR@Q} quoting operator is unavailable, and an
# unquoted name containing a space would corrupt the program.
selection="$(xcrun simctl list devices available --json \
    | OPENTOK_PREFERRED_DEVICE="${SIMULATOR_DEVICE}" python3 -c "
import json, os, sys

preferred = os.environ['OPENTOK_PREFERRED_DEVICE']
runtimes = json.load(sys.stdin)['devices']

def candidates():
    for runtime in sorted(runtimes, reverse=True):
        for device in runtimes[runtime]:
            yield device

for device in candidates():
    if device['name'] == preferred:
        print(device['udid'], device['name'], sep='\t')
        raise SystemExit

for device in candidates():
    if device['name'].startswith('iPhone'):
        print(device['udid'], device['name'], sep='\t')
        raise SystemExit
")"

udid="${selection%%$'\t'*}"
device_name="${selection#*$'\t'}"

if [ -z "${udid}" ]; then
    echo "::error::no available iPhone simulator to run on"
    xcrun simctl list devices available
    exit 1
fi

if [ "${device_name}" != "${SIMULATOR_DEVICE}" ]; then
    echo "==> '${SIMULATOR_DEVICE}' is not available, using '${device_name}'"
fi

echo "==> booting ${device_name} (${udid})"
xcrun simctl boot "${udid}" 2>/dev/null || true
xcrun simctl bootstatus "${udid}" -b

echo "==> installing"
xcrun simctl install "${udid}" "${APP_PATH}"

echo "==> running"
# --console-pty streams the app's stdout straight back, so the verdict rarely needs log scraping.
# The app terminates itself once it has printed OPENTOK_E2E_DONE.
#
# The unified log also holds earlier runs of the app, and a stale PASS from one of those must not
# stand in for this run, so every query below starts at this launch. A unix time, because `log show`
# reads its other formats in the simulator's time zone.
launch_started="$(date +%s)"
set +e
xcrun simctl launch --console-pty "${udid}" "${BUNDLE_ID}" 2>&1 | tee "${LOG_FILE}"
set -e

# Prints PASS or FAIL for the last verdict on stdin, or nothing when there is none. -a because the
# console stream is raw terminal output, and grep answers -o with "Binary file matches" if it
# decides that is not text.
read_verdict() {
    { grep -aEo "OPENTOK_E2E_DONE (PASS|FAIL)" || true; } | tail -1 | cut -d' ' -f2
}

verdict="$(read_verdict < "${LOG_FILE}")"

# simctl can stop relaying when the app exits, with the last lines still undelivered. DatadogNet run
# 41110146674 printed a passing verdict, but the console stream ended half a second of output before
# it and was running two seconds behind the app, so a short wait in the app would not be enough.
# This app does wait a second before exiting, which narrows that gap without closing it. That is a
# lost report rather than a failed run, and the unified log still has it. The PASS and FAIL lines
# come too, so the uploaded log says which check failed. The retries only cost time on a run that is
# failing anyway, and a query that came too early would bring the false failure back.
if [ -z "${verdict}" ]; then
    echo "==> no verdict on the console stream; reading the device log"
    for _ in 1 2 3; do
        xcrun simctl spawn "${udid}" log show --start "@${launch_started}" --predicate \
            "process == '${APP_PROCESS}' AND (eventMessage BEGINSWITH 'PASS ' OR eventMessage BEGINSWITH 'FAIL ' OR eventMessage BEGINSWITH 'OPENTOK_E2E_DONE ')" \
            2>/dev/null | tee -a "${LOG_FILE}" || true
        verdict="$(read_verdict < "${LOG_FILE}")"
        [ -z "${verdict}" ] || break
        sleep 5
    done

    if [ -n "${verdict}" ]; then
        echo "::warning::the console stream lost the verdict, so it was read from the device log"
    fi
fi

if [ "${verdict}" != "PASS" ]; then
    # No verdict usually means the app died before reporting, so keep the crash trace. A missing
    # or mis-stripped xcframework shows up here as a dyld failure naming the framework.
    echo "==> no passing verdict; capturing crash output"
    xcrun simctl spawn "${udid}" log show --start "@${launch_started}" --predicate "process == '${APP_PROCESS}'" \
        2>/dev/null | tail -100 | tee -a "${LOG_FILE}" || true
    echo "::error::OpenTok simulator checks failed or timed out"
    exit 1
fi

echo "==> simulator checks passed"
