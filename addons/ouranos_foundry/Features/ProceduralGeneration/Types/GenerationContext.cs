#nullable enable

using System;
using Ouranos.Foundry.Core.Types;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Types;

/// <summary>
/// What a pipeline hands each pass: the world config and its definitions snapshot, the
/// world being generated, a random stream of the pass's own and a logger gated by
/// <c>WorldGenerator.LogLevel</c>. Touches no Godot API itself.
/// </summary>
public sealed class GenerationContext(
    ProceduralGenerationConfig config,
    GenerationDefinitions definitions,
    WorldState world,
    ulong rngSalt,
    LogLevel logLevel,
    Action<string> log
)
{
    /// <summary>
    /// The config. Off the main thread, read its plain properties only, never its
    /// definition arrays: use <see cref="Definitions"/>.
    /// </summary>
    public ProceduralGenerationConfig Config { get; } = config;

    /// <summary>The config's definition tables and sub-configs, safe on any thread.</summary>
    public GenerationDefinitions Definitions { get; } = definitions;

    public WorldState World { get; } = world;

    /// <summary>
    /// Seeded from <c>World.Seed</c> and the pass's <c>RngSalt</c>, so the same seed and
    /// pass always get the same stream. A chunk pass gets the same stream for every chunk.
    /// </summary>
    public SeededRng Rng { get; } = new(world.Seed, rngSalt);

    public LogLevel LogLevel { get; } = logLevel;

    /// <summary>
    /// Logs when <see cref="LogLevel"/> is at least <paramref name="level"/>. Blueprint
    /// passes print immediately; chunk passes are buffered and printed on the main thread
    /// when the chunk job finishes.
    /// </summary>
    public void Log(string message, LogLevel level = LogLevel.Basic)
    {
        if (LogLevel >= level)
        {
            log(message);
        }
    }
}
