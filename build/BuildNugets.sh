#!/bin/sh

set -e

# Builds and packs every package listed in build/packages.tsv: OpenTok.Net (the cross-platform
# Session/Publisher/Subscriber API) and OpenTok.Net.Maui (the video view that sits on it).
#
# Usage:
#   ./build/BuildNugets.sh                     # version from Directory.Build.props
#   ./build/BuildNugets.sh 2.34.1.1-beta.1     # explicit package version
#
# Both packages share one version line, so a single version argument applies to both — unlike the
# Android binding repository, whose two packages sit on independent native version lines and
# therefore take a --suffix instead.
#
# Nothing native is built here. OpenTok.Net.iOS and OpenTok.Net.Android are built and published from
# their own repositories and restored as pinned dependencies; NuGet.config points at sibling
# checkouts' artifacts/ ahead of nuget.org, so packing against an unreleased binding change means
# packing that repository first.
#
# Requires macOS: an -ios target framework needs Xcode and the ios workload to resolve its reference
# assemblies even for a project with no native code of its own, and there is no cross-platform path
# for the Apple toolchain.
#
# Each .NET SDK's android/ios workloads ship reference packs for only two target framework bands —
# the .NET 9 band covers net8/net9, the .NET 10 band covers net10 alone for this pair of platforms —
# so this runs two passes and merges them with build/merge-packages.py. The repository's global.json
# pins the .NET 9 SDK, so the second pass is invoked from a scratch directory carrying its own
# global.json, since the SDK is resolved from the working directory.

cd "$(dirname "$0")"

VERSION="$1"
ROOT="$(cd .. && pwd)"
OUTPUT="$ROOT/artifacts"

PASS1_BAND="net9"
PASS2_BAND="net10"
PASS2_SDK="10.0.100"

# Column 1 is the package id, which is also the directory under src/ and the .csproj basename.
PACKAGES=$(grep -v '^#' packages.tsv | grep -v '^[[:space:]]*$' | cut -f1)

if [ -z "$PACKAGES" ]; then
    echo "error: no packages found in build/packages.tsv" >&2
    exit 1
fi

VERSION_ARG=""
if [ -n "$VERSION" ]; then
    # Validated before being interpolated into MSBuild arguments and package file names.
    case "$VERSION" in
        *[!A-Za-z0-9.+_-]*)
            echo "error: invalid version '$VERSION'" >&2
            exit 1
            ;;
    esac
    VERSION_ARG="-p:Version=$VERSION"
fi

# NuGet.config declares ./artifacts as a package source, and restore fails outright with NU1301 if a
# local source directory is missing — before anything here has had a chance to create it as an
# output directory. A fresh clone only has it because an empty .gitkeep is committed, so make the
# build independent of that surviving.
mkdir -p "$OUTPUT"

PASS1_DIR="$OUTPUT/.net9-pass"
PASS2_DIR="$OUTPUT/.net10-pass"

SDK10_DIR="$(mktemp -d)"
trap 'rm -rf "$SDK10_DIR" "$PASS1_DIR" "$PASS2_DIR"' EXIT
cat > "$SDK10_DIR/global.json" <<EOF
{ "sdk": { "version": "$PASS2_SDK", "rollForward": "latestFeature" } }
EOF

# Packed and merged one package at a time, in packages.tsv's dependency order, rather than both
# bands of every package followed by a single merge at the end.
#
# OpenTok.Net.Maui references OpenTok.Net as a *package*, not a project — so its restore reads
# artifacts/. Even its net9-band pass cross-targets four target frameworks at once, so it needs an
# OpenTok.Net there that already carries every one of them. A merge deferred to the end cannot
# provide that in time; a per-package merge, run immediately after that package's own two passes,
# can.
for package in $PACKAGES; do
    project="$ROOT/src/$package/$package.csproj"

    if [ ! -f "$project" ]; then
        echo "error: $project does not exist, but build/packages.tsv lists $package" >&2
        exit 1
    fi

    rm -rf "$PASS1_DIR" "$PASS2_DIR"

    echo "==> packing $package ($PASS1_BAND band)"
    dotnet pack "$project" \
        -c Release \
        -p:OpenTokSdkBand="$PASS1_BAND" \
        $VERSION_ARG \
        -o "$PASS1_DIR"

    echo "==> packing $package ($PASS2_BAND band)"
    (cd "$SDK10_DIR" && dotnet pack "$project" \
        -c Release \
        -p:OpenTokSdkBand="$PASS2_BAND" \
        $VERSION_ARG \
        -o "$PASS2_DIR")

    echo "==> merging target frameworks for $package"
    python3 "$ROOT/build/merge-packages.py" "$PASS1_DIR" "$PASS2_DIR" "$OUTPUT"
done

rm -rf "$PASS1_DIR" "$PASS2_DIR"
