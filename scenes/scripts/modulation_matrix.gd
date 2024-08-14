class_name ModulationMatrix
extends PanelContainer

var mod_entry = preload("res://scenes/ModulationEntry.tscn")

var previous_connections:Array
# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	pass # Replace with function body.


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass

func _on_apply_connections_button_pressed() -> void:
	_remove_existing_connections();
	_add_new_connections()

			
func _add_new_connections() ->  void:
	for entry in %ModEntryVBoxContainer.get_children():
		if entry is ModulationEntry:
			var src_name = get_node_name_from_textbox_entry(entry.get_source_name())
			var dst_name = get_node_name_from_textbox_entry(entry.get_destination_name())
			var param_name = get_node_param_from_textbox_entry(entry.get_parameter_name())
			%AudioOutputNode.Connect(src_name, dst_name, param_name)
			previous_connections.append({src = src_name, dst = dst_name, param = param_name})
							
func _remove_existing_connections() -> void:
	for entry in previous_connections:
		var src_name = entry.src
		var dst_name = entry.dst
		var param_name = entry.param
		%AudioOutputNode.Disconnect(src_name, dst_name, param_name)
	previous_connections.clear()

func get_node_param_from_textbox_entry(node_name) -> String:
		match node_name:
			"Pitch":
				return "Frequency"
			"Phase":
				return "Phase"
		return "unknown"

func get_node_name_from_textbox_entry(node_name) -> String:
		match node_name:
			"Oscillator 1":
				return "Osc1"
			"Oscillator 2":
				return "Osc2"
			"Oscillator 3":
				return "Osc3"
			"Oscillator 4":
				return "Osc4"
			"Oscillator 5":
				return "Osc5"
		return "unknown"

func _on_add_connection_button_pressed() -> void:
	var entry = mod_entry.instantiate()
	%ModEntryVBoxContainer.add_child(entry)