class_name ModulationMatrix
extends PanelContainer

var mod_entry = preload("res://scenes/ModulationEntry.tscn")

var previous_connections:Array

func _on_apply_connections_button_pressed() -> void:
	_remove_existing_connections();
	_add_new_connections()
			
func _add_new_connections() ->  void:
	for entry in %ModEntryVBoxContainer.get_children():
		if entry is ModulationEntry:
			var src_name = get_node_name_from_textbox_entry(entry.get_source_name())
			var dst_name = get_node_name_from_textbox_entry(entry.get_destination_name())
			var param_name = get_node_param_from_textbox_entry(entry.get_parameter_name())
			if (src_name.begins_with("Envelope")):
				%AudioOutputNode.Connect(src_name, dst_name, param_name, 1, 1.0)
			else:
				%AudioOutputNode.Connect(src_name, dst_name, param_name, 0, 1.0)
			
			previous_connections.append({src = src_name, dst = dst_name, param = param_name})
		#%AudioOutputNode.PrepareGraph();
							
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
				return "Pitch"
			"Gain":
				return "Gain"
			"Phase":
				return "Phase"
			"PhaseMod":
				return "PMod"
			"Stereo Balance":
				return "Balance"
			"PWM":
				return "PWM"
			"Cutoff":
				return "CutOffMod"
				
		return "unknown"

func get_node_name_from_textbox_entry(node_name) -> String:
		match node_name:
			"Oscillator 1":
				return "Osc0"
			"Oscillator 2":
				return "Osc1"
			"Oscillator 3":
				return "Osc2"
			"Oscillator 4":
				return "Osc3"
			"Oscillator 5":
				return "Osc4"
			"LFO 1":
				return "LFO0"
			"LFO 2":
				return "LFO1"
			"LFO 3":
				return "LFO2"
			"LFO 4":
				return "LFO3"
			"Envelope 1":
				return "Envelope1"
			"Envelope 2":
				return "Envelope2"
			"Envelope 3":
				return "Envelope3"
			"Envelope 4":
				return "Envelope4"
			"Envelope 5":
				return "Envelope5"								
			"Filter":
				return "MoogFilter"
		return "unknown"

func _on_add_connection_button_pressed() -> void:
	var entry = mod_entry.instantiate()
	entry.delete_pressed.connect(_on_entry_deleted)
	%ModEntryVBoxContainer.add_child(entry)

func _on_entry_deleted(entry:Node) -> void:
	var src_name = get_node_name_from_textbox_entry(entry.get_source_name())
	var dst_name = get_node_name_from_textbox_entry(entry.get_destination_name())
	var param_name = get_node_param_from_textbox_entry(entry.get_parameter_name())
	%AudioOutputNode.Disconnect(src_name, dst_name, param_name)	
	entry.queue_free()
	pass
	
func _on_debug_button_pressed() -> void:
	%AudioOutputNode.Debug()
