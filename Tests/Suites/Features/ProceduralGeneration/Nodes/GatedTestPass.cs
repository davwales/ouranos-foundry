using System;
using System.Threading;
using Ouranos.Foundry.Features.ProceduralGeneration.Nodes;
using Ouranos.Foundry.Features.ProceduralGeneration.Resources;
using Ouranos.Foundry.Features.ProceduralGeneration.Types;

namespace Ouranos.Foundry.Tests.Features.ProceduralGeneration.Nodes;

/// <summary>
/// Generation pass that sets <see cref="Entered"/> and then blocks its worker
/// thread until the test sets <see cref="Release"/>.
/// </summary>
public partial class GatedTestPass : GenerationPass
{
    private static readonly TimeSpan ReleaseTimeout = TimeSpan.FromSeconds(10);

    private readonly Lock _lock = new();
    private int _running;

    public ManualResetEventSlim Entered { get; } = new();

    public ManualResetEventSlim Release { get; } = new();

    /// <summary>How many times RunPass was entered.</summary>
    public int EnteredCount { get; private set; }

    /// <summary>The most RunPass calls that were ever in progress at once.</summary>
    public int MaxConcurrentRuns { get; private set; }

    public override void RunPass(GenerationContext ctx, ChunkData chunk)
    {
        lock (_lock)
        {
            EnteredCount++;
            _running++;
            MaxConcurrentRuns = Math.Max(MaxConcurrentRuns, _running);
        }

        Entered.Set();
        try
        {
            if (!Release.Wait(ReleaseTimeout))
            {
                throw new TimeoutException("GatedTestPass was never released.");
            }
        }
        finally
        {
            lock (_lock)
            {
                _running--;
            }
        }
    }
}
