namespace Ouranos.Foundry.Features.ProceduralGeneration.Nodes;

/// <summary>
/// Discovers and caches generation passes at startup. WorldGenerator calls
/// GetPasses() to retrieve the pass list; passes are executed directly by
/// the generator rather than through this pipeline.
/// </summary>
[GlobalClass]
[Icon("res://Assets/icons/foundry_node.svg")]
public partial class GenerationPipeline : Node
{
    private List<GenerationPass> _passes = [];

    public override void _Ready()
    {
        _passes = [.. GetChildren().OfType<GenerationPass>()];
    }

    /// <summary>
    /// Returns a snapshot of the generation passes. Used by WorldGenerator to
    /// avoid calling GetChildren on a worker thread.
    /// </summary>
    public IReadOnlyList<GenerationPass> GetPasses()
    {
        return _passes.AsReadOnly();
    }
}
