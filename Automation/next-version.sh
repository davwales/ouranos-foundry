#!/usr/bin/env sh

# Prints the next release version, <godot>.<N>: <godot> is the Godot.NET.Sdk
# version the addon is built against, and <N> is one more than the highest
# existing v<godot>.<N> tag (0 if there is none). Fails if the Godot version
# pinned in the csproj files and CI workflows disagree.
#
# Usage: Automation/next-version.sh   (needs tags fetched: git fetch --tags)

set -e

cd "$(git rev-parse --show-toplevel)"

sdk_version() {
  sed -n 's#.*Sdk="Godot.NET.Sdk/\([^"]*\)".*#\1#p' "$1"
}

ci_version() {
  sed -n 's#^ *version: *\([0-9.]*\)$#\1#p' "$1" | sort -u
}

godot="$(sdk_version OuranosFoundry.csproj)"
failed=0
for pinned in \
  "Tests/OuranosFoundry.Tests.csproj=$(sdk_version Tests/OuranosFoundry.Tests.csproj)" \
  ".github/workflows/ci.yml=$(ci_version .github/workflows/ci.yml)" \
  ".github/workflows/package.yml=$(ci_version .github/workflows/package.yml)"; do
  if [ "${pinned#*=}" != "$godot" ]; then
    echo "${pinned%%=*} pins Godot '${pinned#*=}', but OuranosFoundry.csproj uses '$godot'." >&2
    failed=1
  fi
done
if [ "$failed" -ne 0 ]; then
  exit 1
fi

last="$(git tag --list "v$godot.*" | sed "s#^v$godot\.##" | grep -E '^[0-9]+$' | sort -n | tail -n 1)"
if [ -z "$last" ]; then
  echo "$godot.0"
else
  echo "$godot.$((last + 1))"
fi
