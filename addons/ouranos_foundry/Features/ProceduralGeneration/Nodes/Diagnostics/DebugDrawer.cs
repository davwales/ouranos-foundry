#nullable enable

using Godot;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes.Diagnostics;

[GlobalClass]
public abstract partial class DebugDrawer : Node
{
    /// <summary>
    /// Called by the overlay's _Process when the world state reference changes
    /// (new blueprint generated or state replaced). Null means no state available.
    /// </summary>
    public virtual void OnWorldStateChanged(WorldState? world) { }

    /// <summary>
    /// Draw debugging visuals onto the given canvas. Called during _Draw.
    /// </summary>
    public abstract void Draw(
        CanvasItem canvas,
        WorldState world,
        ProceduralGenerationConfig? config
    );

    /// <summary>
    /// Requests a redraw on the parent CanvasItem, if one exists.
    /// </summary>
    protected void RequestRedraw()
    {
        (GetParent() as CanvasItem)?.QueueRedraw();
    }

    /// <summary>
    /// Draws a string with a dark outline for legibility against any background.
    /// </summary>
    protected static void DrawOutlinedString(
        CanvasItem canvas,
        Vector2 position,
        string text,
        int fontSize,
        Color fillColor
    )
    {
        var font = ThemeDB.FallbackFont;
        var outline = new Color(0, 0, 0, 0.7f);
        var offset = 1f;
        font.DrawString(
            canvas.GetCanvasItem(),
            position + new Vector2(-offset, 0),
            text,
            HorizontalAlignment.Center,
            -1,
            fontSize,
            outline
        );
        font.DrawString(
            canvas.GetCanvasItem(),
            position + new Vector2(offset, 0),
            text,
            HorizontalAlignment.Center,
            -1,
            fontSize,
            outline
        );
        font.DrawString(
            canvas.GetCanvasItem(),
            position + new Vector2(0, -offset),
            text,
            HorizontalAlignment.Center,
            -1,
            fontSize,
            outline
        );
        font.DrawString(
            canvas.GetCanvasItem(),
            position + new Vector2(0, offset),
            text,
            HorizontalAlignment.Center,
            -1,
            fontSize,
            outline
        );
        font.DrawString(
            canvas.GetCanvasItem(),
            position,
            text,
            HorizontalAlignment.Center,
            -1,
            fontSize,
            fillColor
        );
    }
}
