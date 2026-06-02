namespace Ouranos.Foundry.Features.ProceduralGeneration.Resources;

/// <summary>
/// Single source of truth for chunk dimensions. Export once on WorldGenerator;
/// all generation passes read from it to ensure consistent chunk sizing.
/// </summary>
[GlobalClass]
public partial class ChunkConfig : Resource
{
    [Export]
    public int TileSize { get; set; } = 16;

    [Export]
    public int ViewDistance { get; set; } = 3;
}
