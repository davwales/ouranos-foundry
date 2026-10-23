#nullable enable

namespace Ouranos.Foundry.Features.ProceduralGeneration.Utils;

/// <summary>
/// Builds the messages that the config resources' <c>Validate()</c> methods return. Each
/// check returns null when the value is fine, so a resource can list its checks and keep
/// the non-null results.
/// </summary>
public static class ConfigValidation
{
    /// <summary><c>"{owner}.{field} ({value}) must be at least {min}."</c>, or null.</summary>
    public static string? AtLeast(string owner, string field, int value, int min)
    {
        return value < min ? $"{owner}.{field} ({value}) must be at least {min}." : null;
    }

    /// <summary><c>"{owner}.{field} is empty."</c> when <paramref name="count"/> is 0, or null.</summary>
    public static string? NotEmpty(string owner, string field, int count)
    {
        return count == 0 ? $"{owner}.{field} is empty." : null;
    }
}
