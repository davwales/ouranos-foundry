namespace Ouranos.Foundry.Core.Attributes;

/// <summary>
/// Checks that every <see cref="RequiresPassesAttribute"/> dependency appears
/// earlier in an ordered pass list.
/// </summary>
public static class RequiresPassesValidator
{
    /// <summary>
    /// Returns one message per unsatisfied dependency; empty when the order is valid.
    /// </summary>
    public static IReadOnlyList<string> FindViolations(IEnumerable<Type> orderedPassTypes)
    {
        var violations = new List<string>();
        var visitedTypes = new HashSet<Type>();

        foreach (var passType in orderedPassTypes)
        {
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
                        violations.Add(
                            $"{passType.Name} requires {required.Name} to run before it, "
                                + $"but {required.Name} was not found earlier in the pipeline."
                        );
                    }
                }
            }

            visitedTypes.Add(passType);
        }

        return violations;
    }
}
