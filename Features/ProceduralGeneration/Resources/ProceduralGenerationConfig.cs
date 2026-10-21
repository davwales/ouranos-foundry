#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;
using Ouranos.Foundry.Features.ProceduralGeneration.Utils;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

/// <summary>
/// Top-level configuration bundle for procedural world generation.
/// One asset per world - drag into WorldGenerator to configure all settings.
/// Passes read the definitions through <c>GenerationContext.Definitions</c>, a
/// snapshot that is safe on worker threads.
/// </summary>
[GlobalClass]
public partial class ProceduralGenerationConfig : Resource
{
    [ExportCategory("World")]
    [Export]
    public ChunkConfig? ChunkConfig { get; set; }

    [Export]
    public TerrainConfig? TerrainConfig { get; set; }

    [Export]
    public NamingConfig? NamingConfig { get; set; }

    [Export]
    public GenerationConfig? GenerationConfig { get; set; }

    [ExportCategory("Definitions")]
    [Export]
    public Godot.Collections.Array<BiomeDefinition> BiomeDefinitions { get; set; } = [];

    [Export]
    public Godot.Collections.Array<CitySizeDefinition> CitySizeDefinitions { get; set; } = [];

    [Export]
    public Godot.Collections.Array<PoiTypeDefinition> PoiTypeDefinitions { get; set; } = [];

    [Export]
    public Godot.Collections.Array<RoadTypeDefinition> RoadTypeDefinitions { get; set; } = [];

    /// <summary>
    /// Hub connector road type - the road type used for inter-region hub connections.
    /// </summary>
    [Export]
    public string HubRoadTypeId { get; set; } = "highway";

    /// <summary>
    /// Road type for the roads that connect cities within a region.
    /// </summary>
    [Export]
    public string DefaultRoadTypeId { get; set; } = "dirt";

    /// <summary>
    /// Returns a message for each problem that stops the built-in passes from generating a
    /// world: an invalid sub-config or definition, an empty slot in a definition table, or
    /// no biomes. Empty when the config is valid. <c>WorldGenerator</c> runs no pass while
    /// this returns anything. Override to add checks for a subclass's own fields.
    /// </summary>
    public virtual IEnumerable<string> Validate()
    {
        string?[] noBiomes =
        [
            ConfigValidation.NotEmpty(
                nameof(ProceduralGenerationConfig),
                nameof(BiomeDefinitions),
                BiomeDefinitions.Count
            ),
        ];
        return
        [
            .. ChunkConfig?.Validate() ?? [],
            .. TerrainConfig?.Validate() ?? [],
            .. NamingConfig?.Validate() ?? [],
            .. GenerationConfig?.Validate() ?? [],
            .. noBiomes.OfType<string>(),
            .. ValidateTable(nameof(BiomeDefinitions), BiomeDefinitions, d => d.Validate()),
            .. ValidateTable(nameof(CitySizeDefinitions), CitySizeDefinitions, d => d.Validate()),
            .. ValidateTable(nameof(PoiTypeDefinitions), PoiTypeDefinitions, d => d.Validate()),
            .. ValidateTable(nameof(RoadTypeDefinitions), RoadTypeDefinitions, _ => []),
        ];
    }

    /// <summary>
    /// Returns a message for each setting that generation works around with a default,
    /// which is probably not what was meant: an empty city size, POI type or road type
    /// table, or an Id that no definition has. Only meaningful once <c>Validate()</c>
    /// returns nothing.
    /// </summary>
    public virtual IEnumerable<string> GetWarnings()
    {
        const string owner = nameof(ProceduralGenerationConfig);
        var emptyTables = new[]
        {
            (
                nameof(CitySizeDefinitions),
                CitySizeDefinitions.Count,
                "every city uses the default size"
            ),
            (
                nameof(PoiTypeDefinitions),
                PoiTypeDefinitions.Count,
                "cities get no points of interest"
            ),
            (
                nameof(RoadTypeDefinitions),
                RoadTypeDefinitions.Count,
                "every road uses the default type"
            ),
        };
        foreach (var (field, count, consequence) in emptyTables)
        {
            if (count == 0)
            {
                yield return $"{owner}.{field} is empty, so {consequence}.";
            }
        }

        foreach (var id in new GenerationDefinitions(this).UnknownIds)
        {
            yield return $"{owner} refers to unknown {id}. The default is used.";
        }
    }

    private static IEnumerable<string> ValidateTable<T>(
        string field,
        IList<T> table,
        Func<T, IEnumerable<string>> validate
    )
        where T : Resource
    {
        for (var i = 0; i < table.Count; i++)
        {
            var definition = table[i];
            if (definition is null)
            {
                yield return $"{nameof(ProceduralGenerationConfig)}.{field}[{i}] is empty.";
                continue;
            }

            foreach (var message in validate(definition))
            {
                yield return message;
            }
        }
    }
}
