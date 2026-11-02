using Godot;
using Ouranos.Foundry.Features.Persistence.Nodes;

namespace Ouranos.Foundry.Tests.Features.Persistence.Nodes;

/// <summary>Upgrades older saves by prefixing every entry's "label" with "migrated ".</summary>
public partial class MigratingSaveManager : SaveManager
{
    public int? MigratedFrom { get; private set; }

    protected override Godot.Collections.Dictionary<string, Variant> Migrate(
        Godot.Collections.Dictionary<string, Variant> data,
        int fromVersion
    )
    {
        MigratedFrom = fromVersion;
        foreach (var entry in data.Values)
        {
            var fields = entry.AsGodotDictionary();
            fields["label"] = $"migrated {fields["label"]}";
        }

        return data;
    }
}
