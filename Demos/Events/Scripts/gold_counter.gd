extends Label
## A GDScript listener: its EventBusBridge child re-emits "chest_opened" events from the
## C# EventBus, and the scene connects that signal here.

var gold := 0


func _on_chest_opened(_name: StringName, payload: Variant) -> void:
	gold += payload
	text = "Gold (GDScript): %d" % gold
