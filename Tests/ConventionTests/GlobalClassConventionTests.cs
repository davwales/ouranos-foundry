using System.Linq;
using System.Reflection;
using Godot;
using Xunit;

namespace Ouranos.Foundry.Tests.ConventionTests;

public class GlobalClassConventionTests
{
    [Fact]
    public void AllPublicNodeAndResourceSubclasses_MustHaveGlobalClassAttribute()
    {
        var assembly = typeof(Ouranos.Foundry.Features.Example.Nodes.ExampleGreeter).Assembly;

        var violations = assembly
            .GetTypes()
            .Where(t =>
                t.IsClass
                && t.IsPublic
                && !t.IsAbstract
                && t.GetCustomAttribute<GlobalClassAttribute>() is null
                && (typeof(Node).IsAssignableFrom(t) || typeof(Resource).IsAssignableFrom(t))
            )
            .Select(t => t.FullName)
            .ToList();

        Assert.Empty(violations);
    }
}
