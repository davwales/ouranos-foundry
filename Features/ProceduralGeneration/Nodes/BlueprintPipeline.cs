using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Ouranos.Foundry.Core.Attributes;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes;

[GlobalClass]
[Icon("res://Assets/icons/foundry_node.svg")]
public partial class BlueprintPipeline : Node
{
    [Signal]
    public delegate void PassStartedEventHandler(string passName);

    [Signal]
    public delegate void PassCompletedEventHandler(string passName);

    [Signal]
    public delegate void PassFailedEventHandler(string passName, string error);

    private List<BlueprintPass> _passes = [];

    public override void _Ready()
    {
        CachePasses();
    }

    private void CachePasses()
    {
        _passes = [.. GetChildren().OfType<BlueprintPass>()];
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
        var pass = new T();
        AddChild(pass);
        CachePasses();
    }

    /// <summary>
    /// Validates that all passes declared via RequiresPassesAttribute have
    /// already been visited in the pipeline order. Throws if a required
    /// pass hasn't run yet.
    /// </summary>
    public void ValidateDependencies()
    {
        var visitedTypes = new HashSet<Type>();

        foreach (var pass in _passes)
        {
            var passType = pass.GetType();
            var attr =
                passType
                    .GetCustomAttributes(typeof(RequiresPassesAttribute), false)
                    .FirstOrDefault() as RequiresPassesAttribute;

            if (attr is not null)
            {
                foreach (var required in attr.RequiredPasses)
                {
                    if (!visitedTypes.Contains(required))
                    {
                        throw new InvalidOperationException(
                            $"{passType.Name} requires {required.Name} to run before it, "
                                + $"but {required.Name} was not found earlier in the pipeline."
                        );
                    }
                }
            }

            visitedTypes.Add(passType);
        }
    }

    public void RunPasses(WorldBlueprint worldBlueprint)
    {
        ValidateDependencies();

        foreach (var pass in _passes)
        {
            var passName = pass.GetType().Name;
            try
            {
                EmitSignal(SignalName.PassStarted, passName);
                pass.RunPass(worldBlueprint);
                EmitSignal(SignalName.PassCompleted, passName);
            }
            catch (Exception e)
            {
                EmitSignal(SignalName.PassFailed, passName, e.Message);
            }
        }
    }

    /// <summary>
    /// Convenience overload that accepts WorldState directly.
    /// </summary>
    public void RunPasses(WorldState worldState)
    {
        RunPasses((WorldBlueprint)worldState);
    }
}
