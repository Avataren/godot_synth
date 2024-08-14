class_name ModulationEntry
extends HBoxContainer

func _ready() -> void:
	_on_destination_option_item_selected(0)
	pass

func _on_destination_option_item_selected(index: int) -> void:
	var itemName = %SourceOption.get_item_text(index);
	%DestinationParameterOption.clear()
	print ("Selected item ", itemName)
	if itemName.begins_with("Oscillator"):
		_populate_oscillator_Parameters()
	elif itemName.begins_with("LFO"):
		_populate_lfo_Parameters()
	elif itemName.begins_with("Envelope"):
		_populate_envelope_Parameters()
	elif itemName.begins_with("Filter"):
		_populate_filter_Parameters()

func _populate_oscillator_Parameters():
	%DestinationParameterOption.add_item("Pitch")
	%DestinationParameterOption.add_item("Phase")
	pass
		
func _populate_lfo_Parameters():
	pass	

func _populate_envelope_Parameters():
	pass
	
func _populate_filter_Parameters():
	pass		

func get_source_name():
	return %SourceOption.get_item_text(%SourceOption.get_selected_id())
	
func get_destination_name():
	return %DestinationOption.get_item_text(%DestinationOption.get_selected_id())
	
func get_parameter_name():
	return %DestinationParameterOption.get_item_text(%DestinationParameterOption.get_selected_id())


func _on_destination_parameter_option_item_selected(index: int) -> void:
	pass # Replace with function body.