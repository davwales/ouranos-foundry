#nullable enable

using Godot;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Types;

/// <summary>
/// What a pipeline hands each pass: the world config, the world being generated, a
/// random stream of the pass's own and a logger gated by <c>WorldGenerator.LogLevel</c>.
/// </summary>
public sealed class GenerationContext(
    ProceduralGenerationConfig config,
    WorldState world,
    ulong rngSalt,
    LogLevel logLevel = LogLevel.Basic
)
{
    public ProceduralGenerationConfig Config { get; } = config;

    public WorldState World { get; } = world;

    /// <summary>
    /// Seeded from <c>World.Seed</c> and the pass's <c>RngSalt</c>, so the same seed and
    /// pass always get the same stream. A chunk pass gets the same stream for every chunk.
    /// </summary>
    public SeededRng Rng { get; } = new(world.Seed, rngSalt);

    public LogLevel LogLevel { get; } = logLevel;

    public void Log(string message, LogLevel level = LogLevel.Basic)
    {
        if (LogLevel >= level)
        {
            GD.Print(message);
        }
    }
}
