using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Diagnostics;

[GlobalClass]
public partial class ChunkGridDrawer : DebugDrawer
{
    [Export]
    public bool Visible
    {
        get;
        private set
        {
            field = value;
            RequestRedraw();
        }
    } = true;

    [Export]
    public Color GridColor
    {
        get;
        private set
        {
            field = value;
            RequestRedraw();
        }
    } = new(0.3f, 0.3f, 0.3f, 0.3f);

    public override void Draw(
        CanvasItem canvas,
        WorldState world,
        ProceduralGenerationConfig? config
    )
    {
        if (!Visible)
        {
            return;
        }

        var worldSize = world.Size;
        var chunkSize = world.ChunkSize;

        for (var x = 0; x <= worldSize; x += chunkSize)
        {
            canvas.DrawLine(new Vector2(x, 0), new Vector2(x, worldSize), GridColor);
        }

        for (var y = 0; y <= worldSize; y += chunkSize)
        {
            canvas.DrawLine(new Vector2(0, y), new Vector2(worldSize, y), GridColor);
        }
    }
}
