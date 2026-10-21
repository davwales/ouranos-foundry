#nullable enable

using System.Linq;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;
using Ouranos.Foundry.Features.ProceduralGeneration.Utils;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes;

[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class BlueprintPipeline : Node
{
    [Signal]
    public delegate void PassStartedEventHandler(string passName);

    [Signal]
    public delegate void PassCompletedEventHandler(string passName);

    [Signal]
    public delegate void PassFailedEventHandler(string passName, string error);

    private PassSequence<BlueprintPass> _passes = new([]);

    public override void _Ready()
    {
        CachePasses();
    }

    private void CachePasses()
    {
        _passes = new([.. GetChildren().OfType<BlueprintPass>()]);
    }

    /// <summary>
    /// Adds a pass to the pipeline as a child node and updates the cache.
    /// </summary>
    public void AddPass(BlueprintPass pass)
    {
        AddChild(pass);
        CachePasses();
    }

    /// <summary>
    /// Creates and adds a pass of type T to the pipeline.
    /// </summary>
    public void AddPass<T>()
        where T : BlueprintPass, new()
    {
        AddPass(new T());
    }

    /// <summary>
    /// Runs the passes in child order. Stops at the first failure (a dependency
    /// violation or a pass that throws), emits <c>PassFailed</c> and returns false.
    /// </summary>
    public bool RunPasses(
        ProceduralGenerationConfig config,
        GenerationDefinitions definitions,
        WorldState world,
        LogLevel logLevel = LogLevel.Basic
    )
    {
        var failure = _passes.Run(
            nameof(BlueprintPipeline),
            pass =>
            {
                var passName = pass.GetType().Name;
                EmitSignal(SignalName.PassStarted, passName);
                pass.RunPass(
                    new GenerationContext(config, definitions, world, pass.Salt, logLevel, GD.Print)
                );
                EmitSignal(SignalName.PassCompleted, passName);
            }
        );

        if (failure is { } f)
        {
            EmitSignal(SignalName.PassFailed, f.PassName, f.Error);
            return false;
        }

        return true;
    }
}
