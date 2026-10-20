#!/usr/bin/env sh

# End-to-end check that the addon works in a game: generates a throwaway Godot C#
# project with a default .csproj (no global usings, nullable off), installs the
# addon the way a consumer would, builds with warnings as errors, and runs a
# scene that uses Foundry nodes.
#
# Usage:
#   GODOT_BIN=/path/to/Godot Automation/consumer-smoke.sh submodule <repo url or path> [branch]
#   GODOT_BIN=/path/to/Godot Automation/consumer-smoke.sh zip <path to release zip>

set -e

mode="$1"
source="$2"
branch="${3:-addon}"

if [ -z "$GODOT_BIN" ] || [ -z "$mode" ] || [ -z "$source" ]; then
  echo "Usage: GODOT_BIN=... $0 submodule <repo> [branch] | zip <file>" >&2
  exit 2
fi

repo_root="$(cd "$(dirname "$0")/.." && pwd)"
sdk="$(sed -n 's#.*Sdk="Godot.NET.Sdk/\([^"]*\)".*#\1#p' "$repo_root/OuranosFoundry.csproj")"
case "$source" in
  /* | *://*) ;;
  *) source="$(pwd)/$source" ;;
esac

game="$(mktemp -d)"
trap 'rm -rf "$game"' EXIT
cd "$game"
echo "=== Creating consumer project in $game (Godot.NET.Sdk/$sdk) ==="

cat > project.godot <<'EOF'
config_version=5

[application]

config/name="ConsumerSmoke"
run/main_scene="res://main.tscn"
config/features=PackedStringArray("4.6", "C#")

[dotnet]

project/assembly_name="ConsumerSmoke"
EOF

cat > ConsumerSmoke.csproj <<EOF
<Project Sdk="Godot.NET.Sdk/$sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <EnableDynamicLoading>true</EnableDynamicLoading>
  </PropertyGroup>
</Project>
EOF

cat > Main.cs <<'EOF'
using System.Linq;
using Godot;
using Ouranos.Foundry.Features.Health.Nodes;

public partial class Main : Node
{
    private int _failures;

    public override void _Ready()
    {
        var classes = ProjectSettings.GetGlobalClassList();
        var names = classes.Select(c => (string)c["class"]).ToList();
        Expect(names.Contains("HealthComponent"), "HealthComponent is a global class");
        Expect(names.Contains("LootTableRoller"), "LootTableRoller is a global class");
        Expect(names.Contains("WorldGenerator"), "WorldGenerator is a global class");
        Expect(!names.Contains("TemplateNode"), "template classes are not shipped");
        Expect(!names.Contains("DemoItem"), "demo classes are not shipped");

        var icon = (string)classes.First(c => (string)c["class"] == "HealthComponent")["icon"];
        Expect(ResourceLoader.Exists(icon), $"node icon {icon} resolves");

        var health = GetNode<HealthComponent>("Health");
        health.Damage(25f);
        Expect(health.CurrentHealth == 75f, $"HealthComponent took damage ({health.CurrentHealth})");

        GD.Print(_failures == 0 ? "SMOKE PASSED" : $"SMOKE FAILED ({_failures})");
        GetTree().Quit(_failures == 0 ? 0 : 1);
    }

    private void Expect(bool condition, string description)
    {
        GD.Print((condition ? "ok   " : "FAIL ") + description);
        if (!condition)
        {
            _failures++;
        }
    }
}
EOF

cat > main.tscn <<'EOF'
[gd_scene format=3]

[ext_resource type="Script" path="res://Main.cs" id="1"]
[ext_resource type="Script" path="res://addons/ouranos_foundry/Features/Health/Nodes/HealthComponent.cs" id="2"]

[node name="Main" type="Node"]
script = ExtResource("1")

[node name="Health" type="Node" parent="."]
script = ExtResource("2")
EOF

echo "=== Installing addon ($mode) ==="
case "$mode" in
  submodule)
    git init -q
    git -c protocol.file.allow=always submodule add -q -b "$branch" "$source" addons/ouranos_foundry
    ;;
  zip)
    unzip -q "$source" -d .
    ;;
  *)
    echo "Unknown mode '$mode'." >&2
    exit 2
    ;;
esac

if [ ! -f addons/ouranos_foundry/Features/Health/Nodes/HealthComponent.cs ]; then
  echo "Addon was not installed at addons/ouranos_foundry/." >&2
  exit 1
fi
for unwanted in project.godot OuranosFoundry.csproj Demos Tests Templates; do
  if [ -e "addons/ouranos_foundry/$unwanted" ]; then
    echo "Addon contains '$unwanted', which must not ship." >&2
    exit 1
  fi
done

echo "=== Building (warnings as errors) ==="
dotnet build --nologo -v q -p:TreatWarningsAsErrors=true

echo "=== Importing and running ==="
"$GODOT_BIN" --path . --headless --import > import.log 2>&1 || {
  cat import.log
  exit 1
}
"$GODOT_BIN" --path . --headless 2>&1 | tee run.log
grep -q "SMOKE PASSED" run.log
