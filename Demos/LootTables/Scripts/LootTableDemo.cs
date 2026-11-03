using System.Text;
using Godot;
using Ouranos.Foundry.Features.LootTables.Nodes;
using Ouranos.Foundry.Features.LootTables.Resources;

namespace Ouranos.Foundry.Demos.LootTables;

public partial class LootTableDemo : Control
{
    [Export]
    private LootTableRoller? _roller;

    [Export]
    private RichTextLabel? _resultsLabel;

    [Export]
    private RichTextLabel? _probabilityLabel;

    [Export]
    private Button? _commonChestButton;

    [Export]
    private Button? _bossDropButton;

    [Export]
    private Button? _gachaButton;

    [Export]
    public LootTableData? CommonChestTable { get; set; }

    [Export]
    public LootTableData? BossDropTable { get; set; }

    [Export]
    public LootTableData? GachaTable { get; set; }

    private int _emptyDropCount;

    public override void _Ready()
    {
        _roller?.TableRolled += OnTableRolled;
        _roller?.EmptyDropSelected += OnEmptyDropSelected;

        if (_commonChestButton != null && CommonChestTable != null)
        {
            _commonChestButton.Pressed += () => RollTable(CommonChestTable);
        }

        if (_bossDropButton != null && BossDropTable != null)
        {
            _bossDropButton.Pressed += () => RollTable(BossDropTable);
        }

        if (_gachaButton != null && GachaTable != null)
        {
            _gachaButton.Pressed += () => RollTable(GachaTable);
        }

        if (CommonChestTable != null)
        {
            DisplayProbabilities(CommonChestTable);
        }
    }

    private void RollTable(LootTableData table)
    {
        if (_roller == null)
        {
            return;
        }

        _roller.SetTable(table);
        _emptyDropCount = 0;
        var results = _roller.Roll();

        DisplayResults(table, results, _roller.ActiveSeed);
        DisplayProbabilities(table);
    }

    private void OnTableRolled(LootTableData table, Godot.Collections.Array<LootResult> results)
    {
        _resultsLabel?.AppendText(
            $"[color=gray][signal] TableRolled fired: {table.DisplayName} → {results.Count} result{(results.Count == 1 ? "" : "s")}[/color]\n\n"
        );
    }

    private void OnEmptyDropSelected(LootTableData table)
    {
        _emptyDropCount++;
        _resultsLabel?.AppendText(
            $"[color=gray][signal] EmptyDropSelected fired: {table.DisplayName}[/color]\n"
        );
    }

    private void DisplayResults(
        LootTableData table,
        Godot.Collections.Array<LootResult> results,
        long seed
    )
    {
        if (_resultsLabel == null)
        {
            return;
        }

        _resultsLabel.AppendText(
            $"[color=green]Rolled '{table.DisplayName}' (seed: {seed})[/color]\n"
        );

        foreach (var result in results)
        {
            var itemName = result.Item switch
            {
                DemoItem demoItem => demoItem.DisplayName,
                { } item when !string.IsNullOrEmpty(item.ResourceName) => item.ResourceName,
                _ => "Unknown Item",
            };

            _resultsLabel.AppendText($"  • {itemName} x{result.Quantity}\n");
        }

        _resultsLabel.AppendText(
            $"---\nTotal items: {results.Count} | Empty drops: {_emptyDropCount}\n\n"
        );
    }

    private void DisplayProbabilities(LootTableData table)
    {
        if (_probabilityLabel == null)
        {
            return;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Table: {table.DisplayName}");

        foreach (var prob in table.GetProbabilities())
        {
            var entryName = prob.Entry?.DisplayName ?? prob.EntryId;
            sb.AppendLine($"  {entryName} - {prob.Probability * 100:F1}%");
        }

        _probabilityLabel.Text = sb.ToString();
    }
}
