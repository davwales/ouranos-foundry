#!/usr/bin/env sh

# One-time setup for a fresh clone: restores .NET tools and points git at the
# hooks tracked in Automation/ (pre-commit, pre-push).

set -e

cd "$(git rev-parse --show-toplevel)"

echo "=== Restoring .NET tools ==="
dotnet tool restore --verbosity quiet

echo "=== Installing git hooks ==="
git config core.hooksPath Automation

echo ""
echo "=== Setup complete (core.hooksPath=Automation) ==="
