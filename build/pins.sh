#!/usr/bin/env bash
# The only parser of Directory.Build.props for shell callers. Source this, don't execute it.
#
#   . build/pins.sh
#   echo "$OPENTOK_PACKAGE_VERSION"

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export OPENTOK_REPO_ROOT
OPENTOK_REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"

_pin() {
    grep -oE "<$1>[^<]+" "${OPENTOK_REPO_ROOT}/Directory.Build.props" | head -1 | sed "s/<$1>//"
}

export OPENTOK_VERSION
OPENTOK_VERSION="$(_pin OpenTokVersion)"
export OPENTOK_BINDING_REVISION
OPENTOK_BINDING_REVISION="$(_pin OpenTokBindingRevision)"
export OPENTOK_PACKAGE_VERSION="${OPENTOK_VERSION}.${OPENTOK_BINDING_REVISION}"

# The platform binding packages this façade wraps. Both are consumed as exact versions, so a
# release has to have them on nuget.org (or in a sibling checkout's artifacts/) first.
export OPENTOK_IOS_PACKAGE_VERSION
OPENTOK_IOS_PACKAGE_VERSION="$(_pin OpenTokIosPackageVersion | sed "s/\$(OpenTokVersion)/${OPENTOK_VERSION}/")"
export OPENTOK_ANDROID_PACKAGE_VERSION
OPENTOK_ANDROID_PACKAGE_VERSION="$(_pin OpenTokAndroidPackageVersion | sed "s/\$(OpenTokVersion)/${OPENTOK_VERSION}/")"
