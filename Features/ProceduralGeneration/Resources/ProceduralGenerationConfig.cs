using System;
using Godot;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

/// <summary>
/// Top-level configuration bundle for procedural world generation.
/// One asset per world - drag into WorldGenerator to configure all settings.
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
    /// Looks up a BiomeDefinition by ID (case-insensitive).
    /// Falls back to Default on miss with a warning.
    /// </summary>
    public BiomeDefinition GetBiomeDefinition(string biomeId)
    {
        foreach (var def in BiomeDefinitions)
        {
            if (string.Equals(def.Id, biomeId, StringComparison.OrdinalIgnoreCase))
            {
                return def;
            }
        }

        GD.PushWarning($"Unknown biome ID: '{biomeId}'. Using default.");
        return BiomeDefinition.Default;
    }

    /// <summary>
    /// Looks up a CitySizeDefinition by ID (case-insensitive).
    /// Falls back to Default on miss with a warning.
    /// </summary>
    public CitySizeDefinition GetCitySizeDefinition(string sizeId)
    {
        foreach (var def in CitySizeDefinitions)
        {
            if (string.Equals(def.Id, sizeId, StringComparison.OrdinalIgnoreCase))
            {
                return def;
            }
        }

        GD.PushWarning($"Unknown city size ID: '{sizeId}'. Using default.");
        return CitySizeDefinition.Default;
    }

    /// <summary>
    /// Looks up a PoiTypeDefinition by ID (case-insensitive).
    /// Falls back to Default on miss with a warning.
    /// </summary>
    public PoiTypeDefinition GetPoiTypeDefinition(string poiTypeId)
    {
        foreach (var def in PoiTypeDefinitions)
        {
            if (string.Equals(def.Id, poiTypeId, StringComparison.OrdinalIgnoreCase))
            {
                return def;
            }
        }

        GD.PushWarning($"Unknown POI type ID: '{poiTypeId}'. Using default.");
        return PoiTypeDefinition.Default;
    }

    /// <summary>
    /// Looks up a RoadTypeDefinition by ID (case-insensitive).
    /// Falls back to Default on miss with a warning.
    /// </summary>
    public RoadTypeDefinition GetRoadTypeDefinition(string roadTypeId)
    {
        foreach (var def in RoadTypeDefinitions)
        {
            if (string.Equals(def.Id, roadTypeId, StringComparison.OrdinalIgnoreCase))
            {
                return def;
            }
        }

        GD.PushWarning($"Unknown road type ID: '{roadTypeId}'. Using default.");
        return RoadTypeDefinition.Default;
    }

    /// <summary>
    /// Returns all POI type definition IDs.
    /// </summary>
    public string[] GetPoiTypeIds()
    {
        var result = new string[PoiTypeDefinitions.Count];
        for (var i = 0; i < PoiTypeDefinitions.Count; i++)
        {
            result[i] = PoiTypeDefinitions[i].Id;
        }

        return result;
    }

    /// <summary>
    /// Returns all city size definition IDs.
    /// </summary>
    public string[] GetCitySizeIds()
    {
        var result = new string[CitySizeDefinitions.Count];
        for (var i = 0; i < CitySizeDefinitions.Count; i++)
        {
            result[i] = CitySizeDefinitions[i].Id;
        }

        return result;
    }

    /// <summary>
    /// Returns the first city size ID or null if no definitions exist.
    /// </summary>
    public string? GetDefaultCitySizeId()
    {
        return CitySizeDefinitions.Count > 0 ? CitySizeDefinitions[0].Id : null;
    }
}
