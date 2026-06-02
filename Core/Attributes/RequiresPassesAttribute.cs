namespace Ouranos.Foundry.Core.Attributes;

/// <summary>
/// Declares that a pipeline pass depends on one or more other passes
/// having run before it. Used by BlueprintPipeline and GenerationPipeline
/// to validate pass ordering at startup.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class RequiresPassesAttribute(params Type[] requiredPasses) : Attribute
{
    public Type[] RequiredPasses { get; } = requiredPasses;
}
