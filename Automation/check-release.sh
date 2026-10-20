#!/usr/bin/env sh

# Verifies a release tag before publishing: v<godot>.<N>, where <godot> must
# match every pinned Godot version and the full version must match <Version>
# in OuranosFoundry.csproj and have a CHANGELOG entry.
#
# Usage: Automation/check-release.sh v4.6.3.0

set -e

cd "$(git rev-parse --show-toplevel)"

tag="$1"
version="${tag#v}"
godot="${version%.*}"

if ! echo "$tag" | grep -Eq '^v[0-9]+\.[0-9]+\.[0-9]+\.[0-9]+$'; then
  echo "Tag '$tag' is not of the form v<major>.<minor>.<patch>.<N>." >&2
  exit 1
fi

failed=0

check() {
  # $1: description, $2: actual value, $3: expected value
  if [ "$2" != "$3" ]; then
    echo "$1 is '$2', expected '$3'." >&2
    failed=1
  fi
}

sdk_version() {
  sed -n 's#.*Sdk="Godot.NET.Sdk/\([^"]*\)".*#\1#p' "$1"
}

check "Godot.NET.Sdk in OuranosFoundry.csproj" "$(sdk_version OuranosFoundry.csproj)" "$godot"
check "Godot.NET.Sdk in Tests/OuranosFoundry.Tests.csproj" "$(sdk_version Tests/OuranosFoundry.Tests.csproj)" "$godot"
check "Godot version in .github/workflows/ci.yml" \
  "$(sed -n 's#^ *version: *\([0-9.]*\)$#\1#p' .github/workflows/ci.yml)" "$godot"
check "<Version> in OuranosFoundry.csproj" \
  "$(sed -n 's#.*<Version>\(.*\)</Version>.*#\1#p' OuranosFoundry.csproj)" "$version"

if ! grep -Fq "## [$version]" addons/ouranos_foundry/CHANGELOG.md; then
  echo "addons/ouranos_foundry/CHANGELOG.md has no '## [$version]' section." >&2
  failed=1
fi

if [ "$failed" -ne 0 ]; then
  exit 1
fi

echo "Release $tag is consistent (Godot $godot)."
