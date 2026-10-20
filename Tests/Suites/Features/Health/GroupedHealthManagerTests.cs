using GdUnit4;
using Godot;
using Ouranos.Foundry.Features.Health.Nodes;
using Shouldly;

namespace Ouranos.Foundry.Tests.Features.Health;

[TestSuite]
[RequireGodotRuntime]
public class GroupedHealthManagerTests
{
    private static (
        Node root,
        HealthComponent component1,
        HealthComponent component2,
        GroupedHealthManager manager
    ) CreateManager(
        float baseMaxHealth1 = 100f,
        float startingHealth1 = -1f,
        float baseMaxHealth2 = 100f,
        float startingHealth2 = -1f,
        string labelFormat = "{Name}",
        float barWidth = 200f
    )
    {
        var root = new Node { Name = "TestRoot" };

        var parent1 = new Node();
        parent1.Name = "Entity1";
        var component1 = new HealthComponent()
            .With(HealthComponent.PropertyName.BaseMaxHealth, baseMaxHealth1)
            .With(HealthComponent.PropertyName.StartingHealth, startingHealth1);
        parent1.AddChild(component1);

        var parent2 = new Node();
        parent2.Name = "Entity2";
        var component2 = new HealthComponent()
            .With(HealthComponent.PropertyName.BaseMaxHealth, baseMaxHealth2)
            .With(HealthComponent.PropertyName.StartingHealth, startingHealth2);
        parent2.AddChild(component2);

        var manager = new GroupedHealthManager()
            .With(GroupedHealthManager.PropertyName.LabelFormat, labelFormat)
            .With(GroupedHealthManager.PropertyName.BarWidth, barWidth);

        root.AddChild(parent1);
        root.AddChild(parent2);
        root.AddChild(manager);

        return (root, component1, component2, manager);
    }

    private static VBoxContainer? GetContainer(GroupedHealthManager manager)
    {
        foreach (var child in manager.GetChildren())
        {
            if (child is VBoxContainer vbc)
            {
                return vbc;
            }
        }

        return null;
    }

    [TestCase]
    public void RegisterComponent_ShouldCreateRowWithLabelAndBar()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager();
        component1._Ready();
        manager._Ready();

        // Act
        manager.RegisterComponent(component1);

        // Assert
        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        container.GetChildCount().ShouldBe(1);

        var row = container.GetChild(0);
        row.ShouldBeOfType<HBoxContainer>();
        row.GetChildCount().ShouldBeGreaterThanOrEqualTo(2);

        root.Free();
    }

    [TestCase]
    public void UnregisterComponent_ShouldRemoveRow()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager();
        component1._Ready();
        manager._Ready();
        manager.RegisterComponent(component1);

        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        container.GetChildCount().ShouldBe(1);

        // Act
        manager.UnregisterComponent(component1);

        // Assert
        container.GetChildCount().ShouldBe(0);

        root.Free();
    }

    [TestCase]
    public void HealthChanged_ShouldUpdateFillOnCorrectBar()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager(
            baseMaxHealth1: 100f,
            startingHealth1: 100f
        );
        component1._Ready();
        manager._Ready();
        manager.RegisterComponent(component1);

        // Act
        component1.Damage(50f);

        // Assert
        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        var row = (HBoxContainer)container.GetChild(0);
        var background = (ColorRect)row.GetChild(1);
        var fill = (ColorRect)background.GetChild(0);
        fill.Size.X.ShouldBe(200f * 0.65f * 0.5f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void MaxHealthChanged_ShouldUpdateFillProportion()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager(
            baseMaxHealth1: 100f,
            startingHealth1: 50f
        );
        component1._Ready();
        manager._Ready();
        manager.RegisterComponent(component1);

        // Act
        component1.SetBaseMaxHealth(200f, clampCurrent: false);

        // Assert
        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        var row = (HBoxContainer)container.GetChild(0);
        var background = (ColorRect)row.GetChild(1);
        var fill = (ColorRect)background.GetChild(0);
        fill.Size.X.ShouldBe(200f * 0.65f * 0.25f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void LabelFormat_Name_ShouldShowEntityName()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager(labelFormat: "{Name}");
        component1._Ready();
        manager._Ready();

        // Act
        manager.RegisterComponent(component1);

        // Assert
        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        var row = (HBoxContainer)container.GetChild(0);
        var label = (Label)row.GetChild(0);
        label.Text.ShouldBe("Entity1");

        root.Free();
    }

    [TestCase]
    public void LabelFormat_CurrentAndMax_ShouldShowHealthValues()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager(
            baseMaxHealth1: 100f,
            startingHealth1: 75f,
            labelFormat: "{Current}/{Max}"
        );
        component1._Ready();
        manager._Ready();

        // Act
        manager.RegisterComponent(component1);

        // Assert
        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        var row = (HBoxContainer)container.GetChild(0);
        var label = (Label)row.GetChild(0);
        label.Text.ShouldContain("75");
        label.Text.ShouldContain("100");

        root.Free();
    }

    [TestCase]
    public void LabelFormat_Fraction_ShouldShowPercentage()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager(
            baseMaxHealth1: 100f,
            startingHealth1: 75f,
            labelFormat: "{Fraction}"
        );
        component1._Ready();
        manager._Ready();

        // Act
        manager.RegisterComponent(component1);

        // Assert
        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        var row = (HBoxContainer)container.GetChild(0);
        var label = (Label)row.GetChild(0);
        label.Text.ShouldContain("75%");

        root.Free();
    }

    [TestCase]
    public void RegisterComponent_SameComponentTwice_ShouldBeNoOp()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager();
        component1._Ready();
        manager._Ready();
        manager.RegisterComponent(component1);

        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        container.GetChildCount().ShouldBe(1);

        // Act
        manager.RegisterComponent(component1);

        // Assert
        container.GetChildCount().ShouldBe(1);

        root.Free();
    }

    [TestCase]
    public void UnregisterComponent_UnknownComponent_ShouldBeNoOp()
    {
        // Arrange
        var (root, component1, component2, manager) = CreateManager();
        component1._Ready();
        component2._Ready();
        manager._Ready();

        // Act
        manager.RegisterComponent(component1);
        manager.UnregisterComponent(component2);

        // Assert
        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        container.GetChildCount().ShouldBe(1);

        root.Free();
    }

    [TestCase]
    public void RegisterComponent_TreeExiting_ShouldAutoUnregister()
    {
        // Arrange
        var root = new Node();
        var parent = new Node();
        parent.Name = "TestEntity";
        var component = new HealthComponent()
            .With(HealthComponent.PropertyName.BaseMaxHealth, 100f)
            .With(HealthComponent.PropertyName.StartingHealth, 100f);
        parent.AddChild(component);
        var manager = new GroupedHealthManager().With(
            GroupedHealthManager.PropertyName.LabelFormat,
            "{Name}"
        );
        root.AddChild(parent);
        root.AddChild(manager);
        component._Ready();
        manager._Ready();
        manager.RegisterComponent(component);

        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        container.GetChildCount().ShouldBe(1);

        // Act
        component.EmitSignal(Node.SignalName.TreeExiting);

        // Assert
        container.GetChildCount().ShouldBe(0);

        root.Free();
    }

    [TestCase]
    public void ExitTree_ShouldUnregisterAll()
    {
        // Arrange
        var (root, component1, component2, manager) = CreateManager();
        component1._Ready();
        component2._Ready();
        manager._Ready();
        manager.RegisterComponent(component1);
        manager.RegisterComponent(component2);

        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        container.GetChildCount().ShouldBe(2);

        // Act
        manager._ExitTree();

        // Assert
        container.GetChildCount().ShouldBe(0);

        root.Free();
    }

    [TestCase]
    public void MonitorHealthComponent_ShouldDelegateToRegisterComponent()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager();
        component1._Ready();
        manager._Ready();

        // Act
        manager.MonitorHealthComponent(component1);

        // Assert
        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        container.GetChildCount().ShouldBe(1);

        root.Free();
    }

    [TestCase]
    public void MonitorHealthComponent_Null_ShouldBeNoOp()
    {
        // Arrange
        var (_, _, _, manager) = CreateManager();
        manager._Ready();

        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        var initialCount = container.GetChildCount();

        // Act
        manager.MonitorHealthComponent(null);

        // Assert
        container.GetChildCount().ShouldBe(initialCount);
    }

    [TestCase]
    public void RegisterComponent_TwoComponents_ShouldCreateTwoRows()
    {
        // Arrange
        var (root, component1, component2, manager) = CreateManager();
        component1._Ready();
        component2._Ready();
        manager._Ready();

        // Act
        manager.RegisterComponent(component1);
        manager.RegisterComponent(component2);

        // Assert
        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        container.GetChildCount().ShouldBe(2);

        root.Free();
    }

    [TestCase]
    public void RegisterComponent_CustomLabel_ShouldUseCustomLabel()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager(labelFormat: "{Name}");
        component1._Ready();
        manager._Ready();

        // Act
        manager.RegisterComponent(component1, "CustomBoss");

        // Assert
        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        var row = (HBoxContainer)container.GetChild(0);
        var label = (Label)row.GetChild(0);
        label.Text.ShouldBe("CustomBoss");

        root.Free();
    }

    [TestCase]
    public void HealthChanged_ShouldUpdateLabelWithCurrentValues()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager(
            baseMaxHealth1: 100f,
            startingHealth1: 100f,
            labelFormat: "{Current}/{Max}"
        );
        component1._Ready();
        manager._Ready();
        manager.RegisterComponent(component1);

        // Act
        component1.Damage(30f);

        // Assert
        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        var row = (HBoxContainer)container.GetChild(0);
        var label = (Label)row.GetChild(0);
        label.Text.ShouldContain("70");
        label.Text.ShouldContain("100");

        root.Free();
    }

    [TestCase]
    public void RegisterComponent_ZeroMaxHealth_ShouldSetFillToZero()
    {
        // Arrange
        var root = new Node();
        var parent = new Node { Name = "ZeroEntity" };
        var component = new HealthComponent { MaxHealthProvider = () => 0f }
            .With(HealthComponent.PropertyName.BaseMaxHealth, 0f)
            .With(HealthComponent.PropertyName.StartingHealth, 0f);
        parent.AddChild(component);
        var manager = new GroupedHealthManager()
            .With(GroupedHealthManager.PropertyName.LabelFormat, "{Name}")
            .With(GroupedHealthManager.PropertyName.BarWidth, 200f);
        root.AddChild(parent);
        root.AddChild(manager);
        component._Ready();
        manager._Ready();

        // Act
        manager.RegisterComponent(component);

        // Assert
        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        var row = (HBoxContainer)container.GetChild(0);
        var background = (ColorRect)row.GetChild(1);
        var fill = (ColorRect)background.GetChild(0);
        fill.Size.X.ShouldBe(0f, 0.01f);

        root.Free();
    }

    [TestCase]
    public void RegisterComponent_ShouldApplyFontSizeOverride()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager();
        manager.Set(GroupedHealthManager.PropertyName.FontSize, 18);
        component1._Ready();
        manager._Ready();

        // Act
        manager.RegisterComponent(component1);

        // Assert
        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        var row = (HBoxContainer)container.GetChild(0);
        var label = (Label)row.GetChild(0);
        label.GetThemeFontSize("font_size").ShouldBe(18);

        root.Free();
    }

    [TestCase]
    public void ExitTree_ThenHealthChanged_ShouldNotThrow()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager();
        component1._Ready();
        manager._Ready();
        manager.RegisterComponent(component1);

        // Act
        manager._ExitTree();

        // Assert
        Should.NotThrow(() => component1.Damage(10f));

        root.Free();
    }

    [TestCase]
    public void MaxHealthChanged_WhenLabelFormatHasMax_ShouldUpdateLabelText()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager(
            baseMaxHealth1: 100f,
            startingHealth1: 100f,
            labelFormat: "{Name}: {Current}/{Max}"
        );
        component1._Ready();
        manager._Ready();
        manager.RegisterComponent(component1);

        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        var row = (HBoxContainer)container.GetChild(0);
        var label = (Label)row.GetChild(0);

        // Act
        component1.SetBaseMaxHealth(200f, clampCurrent: false);

        // Assert
        label.Text.ShouldBe("Entity1: 100/200");

        root.Free();
    }

    [TestCase]
    public void RegisterComponent_ThenUnregister_ThenRegisterAgain_ShouldWork()
    {
        // Arrange
        var (root, component1, _, manager) = CreateManager(labelFormat: "{Name}: {Current}");
        component1._Ready();
        manager._Ready();
        manager.RegisterComponent(component1);

        var container = GetContainer(manager);
        container.ShouldNotBeNull();
        container.GetChildCount().ShouldBe(1);

        // Act
        manager.UnregisterComponent(component1);
        manager.RegisterComponent(component1, "Reborn");

        // Assert
        container.GetChildCount().ShouldBe(1);

        var row = (HBoxContainer)container.GetChild(0);
        var label = (Label)row.GetChild(0);
        label.Text.ShouldBe("Reborn: 100");

        component1.Damage(50f);
        label.Text.ShouldBe("Reborn: 50");

        root.Free();
    }
}
