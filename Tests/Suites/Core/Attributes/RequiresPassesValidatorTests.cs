using System;
using GdUnit4;
using Ouranos.Foundry.Core.Attributes;
using Shouldly;

namespace Ouranos.Foundry.Tests.Core.Attributes;

[TestSuite]
public class RequiresPassesValidatorTests
{
    private sealed class PassA;

    [RequiresPasses(typeof(PassA))]
    private sealed class PassB;

    [TestCase]
    public void FindViolations_WhenDependencyRunsFirst_ReturnsEmpty()
    {
        // Arrange
        Type[] order = [typeof(PassA), typeof(PassB)];

        // Act
        var violations = RequiresPassesValidator.FindViolations(order);

        // Assert
        violations.ShouldBeEmpty();
    }

    [TestCase]
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

    [TestCase]
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
