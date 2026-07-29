#!/bin/sh

set -e

# Adds the Windows target frameworks to packages that were already packed on macOS.
#
# Usage (on Windows, from a checkout with artifacts/ already populated):
#   ./build/AddWindowsAssets.sh                     # version from Directory.Build.props
#   ./build/AddWindowsAssets.sh 2.34.1.3-beta.1     # explicit package version, matching the pack
#
# Why this is a second script rather than part of BuildNugets.sh:
#
# OpenTok.Net has three platform heads and no machine can build all of them. The ios head needs
# Xcode, so packing happens on macOS; the windows head needs the Windows SDK reference packs and
# WinUI, which exist only on Windows. src/OpenTok.Net.props therefore appends the -windows target
# frameworks only when the build is running on Windows — so a package produced by BuildNugets.sh on
# a Mac is correct, complete for iOS and Android, and silently has no Windows assets at all.
#
# This closes that gap the same way the net9/net10 band split is closed: pack again on the platform
# that can, then merge with build/merge-packages.py, which copies across any lib/<tfm>/ the primary
# package does not already have and adds the matching nuspec dependency group.
#
# Run it *after* BuildNugets.sh, against the same artifacts/ directory and the same version. The
# result overwrites the packages in place, so running it twice is harmless — the second run finds
# the Windows assets already present and copies nothing.
#
# Both SDK bands are packed for the same reason as BuildNugets.sh: net8 and net9 windows heads come
# from the .NET 9 band and net10 from the .NET 10 band. Unlike ios and android there is no workload
# to install — a -windows target framework needs only the Windows SDK reference pack, which every
# band carries.

cd "$(dirname "$0")"

VERSION="$1"
ROOT="$(cd .. && pwd)"
OUTPUT="$ROOT/artifacts"

PASS1_BAND="net9"
PASS2_BAND="net10"
PASS2_SDK="10.0.100"

case "$(uname -s)" in
    MINGW*|MSYS*|CYGWIN*|Windows_NT) ;;
    *)
        echo "error: this must run on Windows — a -windows target framework cannot be built" >&2
        echo "       anywhere else. Run build/BuildNugets.sh on macOS first, then this here." >&2
        exit 1
        ;;
esac

PACKAGES=$(grep -v '^#' packages.tsv | grep -v '^[[:space:]]*$' | cut -f1)

# The lib/<tfm>/ folders a package carries for Windows, one per line, or nothing at all.
#
# Read from the zip rather than from the merge's own report: merge-packages.py prints the target
# frameworks it *added*, and a package can advertise a target framework in its nuspec while
# carrying no assets for it — which restores, and then fails to compile at whoever picked it.
windows_frameworks() {
    python3 - "$1" <<'PY'
import sys
import zipfile

with zipfile.ZipFile(sys.argv[1]) as package:
    frameworks = {
        name.split("/")[1]
        for name in package.namelist()
        if name.startswith("lib/") and "-windows" in name.split("/")[1] and name.count("/") > 1
    }

print("\n".join(sorted(frameworks)))
PY
}

# Fails unless the package carries Windows assets, naming it and listing what it does carry.
require_windows_frameworks() {
    frameworks=$(windows_frameworks "$1" | tr '\n' ' ')

    if [ -z "$(echo "$frameworks" | tr -d ' ')" ]; then
        echo "error: $(basename "$1") carries no lib/<tfm>/ assets for Windows." >&2
        exit 1
    fi

    echo "    $(basename "$1"): $frameworks"
}

if [ -z "$PACKAGES" ]; then
    echo "error: no packages found in build/packages.tsv" >&2
    exit 1
fi

VERSION_ARG=""
if [ -n "$VERSION" ]; then
    case "$VERSION" in
        *[!A-Za-z0-9.+_-]*)
            echo "error: invalid version '$VERSION'" >&2
            exit 1
            ;;
    esac
    VERSION_ARG="-p:Version=$VERSION"
fi

if [ ! -d "$OUTPUT" ] || [ -z "$(ls "$OUTPUT"/*.nupkg 2>/dev/null)" ]; then
    echo "error: no packages in $OUTPUT. Run build/BuildNugets.sh on macOS first and bring its" >&2
    echo "       artifacts/ here — this script adds to existing packages, it does not create them." >&2
    exit 1
fi

WIN1_DIR="$OUTPUT/.win9-pass"
WIN2_DIR="$OUTPUT/.win10-pass"
PRIMARY_DIR="$OUTPUT/.primary"
MERGED_DIR="$OUTPUT/.merged"

SDK10_DIR="$(mktemp -d)"
trap 'rm -rf "$SDK10_DIR" "$WIN1_DIR" "$WIN2_DIR" "$PRIMARY_DIR" "$MERGED_DIR"' EXIT
cat > "$SDK10_DIR/global.json" <<EOF
{ "sdk": { "version": "$PASS2_SDK", "rollForward": "latestFeature" } }
EOF

# Every package this run merged into, by file name, to re-check once the loop is done.
MERGED_PACKAGES=""

for package in $PACKAGES; do
    project="$ROOT/src/$package/$package.csproj"

    if [ ! -f "$project" ]; then
        echo "error: $project does not exist, but build/packages.tsv lists $package" >&2
        exit 1
    fi

    rm -rf "$WIN1_DIR" "$WIN2_DIR" "$PRIMARY_DIR" "$MERGED_DIR"

    echo "==> packing $package windows heads ($PASS1_BAND band)"
    dotnet pack "$project" \
        -c Release \
        -p:OpenTokSdkBand="$PASS1_BAND" \
        -p:OpenTokWindowsOnly=true \
        $VERSION_ARG \
        -o "$WIN1_DIR"

    echo "==> packing $package windows heads ($PASS2_BAND band)"
    (cd "$SDK10_DIR" && dotnet pack "$project" \
        -c Release \
        -p:OpenTokSdkBand="$PASS2_BAND" \
        -p:OpenTokWindowsOnly=true \
        $VERSION_ARG \
        -o "$WIN2_DIR")

    # merge-packages.py merges every package it finds in the primary directory, so the primary
    # here cannot be artifacts/ itself: that holds all the packages, while the Windows passes hold
    # only the one just packed, and every other id would be reported as having no counterpart.
    # Stage just this package's files — the pass directory names them, so no version is needed.
    echo "==> merging windows target frameworks into $package"
    mkdir -p "$PRIMARY_DIR"
    nupkg=""
    for asset in "$WIN1_DIR"/*.nupkg "$WIN1_DIR"/*.snupkg; do
        [ -f "$asset" ] || continue
        name=$(basename "$asset")
        if [ ! -f "$OUTPUT/$name" ]; then
            echo "error: $name is not in $OUTPUT — run build/BuildNugets.sh on macOS first and" >&2
            echo "       bring its artifacts/ here, at the same version." >&2
            exit 1
        fi
        cp "$OUTPUT/$name" "$PRIMARY_DIR/$name"
        case "$name" in *.nupkg) nupkg="$name" ;; esac
    done

    # Merged in two steps because merge-packages.py takes one additional directory at a time, and
    # into a scratch directory because it will not read and write the same place.
    python3 "$ROOT/build/merge-packages.py" "$PRIMARY_DIR" "$WIN1_DIR" "$MERGED_DIR"
    python3 "$ROOT/build/merge-packages.py" "$MERGED_DIR" "$WIN2_DIR" "$OUTPUT"

    # Straight after the merge rather than only at the end, because the next package is packed
    # against this one out of artifacts/ — so a package that came out of the merge without its
    # Windows assets would otherwise be discovered later, having already been built against.
    require_windows_frameworks "$OUTPUT/$nupkg"
    MERGED_PACKAGES="$MERGED_PACKAGES $nupkg"
done

rm -rf "$WIN1_DIR" "$WIN2_DIR" "$PRIMARY_DIR" "$MERGED_DIR"

# And again over the finished directory. Everything above packs and merges one package while the
# rest sit in the same directory, so "each package was right when it was merged" is not the same
# claim as "every package is still right now".
echo "==> windows assets in $OUTPUT"
for nupkg in $MERGED_PACKAGES; do
    require_windows_frameworks "$OUTPUT/$nupkg"
done
