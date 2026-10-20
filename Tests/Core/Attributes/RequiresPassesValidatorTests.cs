using System;
using Ouranos.Foundry.Core.Attributes;
using Shouldly;
using Xunit;

namespace Ouranos.Foundry.Tests.Core.Attributes;

public class RequiresPassesValidatorTests
{
    private sealed class PassA;

    [RequiresPasses(typeof(PassA))]
    private sealed class PassB;

    [Fact]
    public void FindViolations_WhenDependencyRunsFirst_ReturnsEmpty()
    {
        // Arrange
        Type[] order = [typeof(PassA), typeof(PassB)];

        // Act
        var violations = RequiresPassesValidator.FindViolations(order);

        // Assert
        violations.ShouldBeEmpty();
    }

    [Fact]
    public void FindViolations_WhenDependencyRunsLater_ReturnsViolation()
    {
        // Arrange
        Type[] order = [typeof(PassB), typeof(PassA)];

        // Act
        var violations = RequiresPassesValidator.FindViolations(order);

        // Assert
        violations.Count.ShouldBe(1);
        violations[0].ShouldContain("PassB requires PassA");
    }

    [Fact]
    public void FindViolations_WhenDependencyMissing_ReturnsViolation()
    {
        // Arrange
        Type[] order = [typeof(PassB)];

        // Act
        var violations = RequiresPassesValidator.FindViolations(order);

        // Assert
        violations.Count.ShouldBe(1);
    }
}
