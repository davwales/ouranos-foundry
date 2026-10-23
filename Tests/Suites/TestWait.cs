using System;
using System.Threading.Tasks;
using Godot;
using Shouldly;

namespace Ouranos.Foundry.Tests;

internal static class TestWait
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    /// <summary>Awaits process frames until the condition holds, failing after a timeout.</summary>
    public static async Task Until(Func<bool> condition)
    {
        var tree = (SceneTree)Engine.GetMainLoop();
        var deadline = DateTime.UtcNow + Timeout;
        while (!condition())
        {
            DateTime.UtcNow.ShouldBeLessThan(deadline, "Timed out waiting for the condition.");
            await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
        }
    }
}
