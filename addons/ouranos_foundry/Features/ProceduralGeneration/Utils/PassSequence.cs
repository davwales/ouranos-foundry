#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Ouranos.Foundry.Core.Attributes;

namespace Ouranos.Foundry.Features.ProceduralGeneration.Utils;

/// <summary>
/// The ordered passes of a pipeline, checked once against <c>[RequiresPasses]</c>.
/// Shared by <c>BlueprintPipeline</c> and <c>GenerationPipeline</c>.
/// </summary>
internal sealed class PassSequence<TPass>(IReadOnlyList<TPass> passes)
    where TPass : class
{
    /// <summary>The first unsatisfied dependency, or null when the order is valid.</summary>
    public string? Violation { get; } =
        RequiresPassesValidator.FindViolations(passes.Select(p => p.GetType())).FirstOrDefault();

    /// <summary>
    /// Runs the passes in order and stops at the first failure: a dependency violation
    /// (reported as <paramref name="pipelineName"/>) or a pass that throws. Returns null
    /// on success. Cancellation is checked between passes and is rethrown, not reported.
    /// Touches no Godot API, so it is safe on a worker thread.
    /// </summary>
    public (string PassName, string Error)? Run(
        string pipelineName,
        Action<TPass> runPass,
        CancellationToken cancellationToken = default
    )
    {
        if (Violation is not null)
        {
            return (pipelineName, Violation);
        }

        foreach (var pass in passes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                runPass(pass);
            }
            catch (Exception e) when (!cancellationToken.IsCancellationRequested)
            {
                return (pass.GetType().Name, e.Message);
            }
        }

        return null;
    }
}
