#!/usr/bin/env sh

set -e

echo "=== Restoring .NET tools ==="
dotnet tool restore --verbosity quiet

echo "=== Formatting C# whitespace (CSharpier) ==="
dotnet csharpier format .

echo "=== Formatting C# style (dotnet format style) ==="
dotnet format style --no-restore ouranos-foundry.sln

echo "=== Formatting C# analyzers (dotnet format analyzers) ==="
dotnet format analyzers --no-restore ouranos-foundry.sln

echo ""
echo "=== Formatting complete ==="
