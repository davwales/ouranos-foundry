#nullable enable

using System;
using System.Linq;
using System.Threading;
using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;
using Ouranos.Foundry.Features.ProceduralGeneration.Utils;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes;

/// <summary>
/// Discovers its <c>GenerationPass</c> children in <c>_Ready</c> and runs them on each
/// chunk that <c>WorldGenerator</c> generates.
/// </summary>
[GlobalClass]
[Icon("res://addons/ouranos_foundry/Assets/icons/foundry_node.svg")]
public partial class GenerationPipeline : Node
{
    private PassSequence<GenerationPass> _passes = new([]);

    public override void _Ready()
    {
        _passes = new([.. GetChildren().OfType<GenerationPass>()]);
        if (_passes.Violation is not null)
        {
            GD.PushError($"GenerationPipeline: {_passes.Violation}");
        }
    }

    /// <summary>
    /// Runs the passes on <paramref name="chunk"/> in child order. Safe on a worker thread:
    /// it touches no Godot API, and passes log through <paramref name="log"/>.
    /// Returns the first failure (a dependency violation or a pass that throws), or null.
    /// </summary>
    public string? RunPasses(
        ProceduralGenerationConfig config,
        GenerationDefinitions definitions,
        WorldState world,
        ChunkBuffer chunk,
        LogLevel logLevel,
        Action<string> log,
        CancellationToken cancellationToken = default
    )
    {
        var failure = _passes.Run(
            nameof(GenerationPipeline),
            pass =>
                pass.RunPass(
                    new GenerationContext(config, definitions, world, pass.Salt, logLevel, log),
                    chunk
                ),
            cancellationToken
        );
        return failure?.Error;
    }
}
