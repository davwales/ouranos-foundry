using System;
using Godot;
using Ouranos.Foundry.Core.Types;

namespace Ouranos.Foundry.Tests.Features.Persistence.Nodes;

/// <summary>A consumer-style saveable node holding one value of several Variant types.</summary>
public partial class SampleSaveable : Node, ISaveable
{
    [Export]
    public string SaveKey { get; set; } = "";

    public int Count { get; set; }

    public float Ratio { get; set; }

    public string Label { get; set; } = "";

    public Vector2I Cell { get; set; }

    public int[] Values { get; set; } = [];

    public bool ThrowOnSave { get; set; }

    public bool ThrowOnLoad { get; set; }

    public int LoadCount { get; private set; }

    public Godot.Collections.Dictionary<string, Variant> Save()
    {
        if (ThrowOnSave)
        {
            throw new InvalidOperationException("Save exploded.");
        }

        return new()
        {
            ["count"] = Count,
            ["ratio"] = Ratio,
            ["label"] = Label,
            ["cell"] = Cell,
            ["values"] = Values,
        };
    }

    public void Load(Godot.Collections.Dictionary<string, Variant> data)
    {
        if (ThrowOnLoad)
        {
            throw new InvalidOperationException("Load exploded.");
        }

        Count = data["count"].AsInt32();
        Ratio = data["ratio"].AsSingle();
        Label = data["label"].AsString();
        Cell = data["cell"].AsVector2I();
        Values = data["values"].AsInt32Array();
        LoadCount++;
    }
}
