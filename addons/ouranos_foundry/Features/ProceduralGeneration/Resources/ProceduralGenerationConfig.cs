#nullable enable

using Godot;

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
}
