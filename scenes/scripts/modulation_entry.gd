class_name ModulationEntry
extends HBoxContainer

signal delete_pressed
signal parameter_Selected

func _ready() -> void:
	_on_destination_option_item_selected(0)
	_on_source_option_item_selected(0)

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
	%DestinationParameterOption.add_item("PhaseMod")
	%DestinationParameterOption.add_item("Stereo Balance")
		
func _populate_lfo_Parameters():
	%DestinationParameterOption.add_item("Depth")
	%DestinationParameterOption.add_item("Rate")

func _populate_envelope_Parameters():
	%DestinationParameterOption.add_item("Attack")
	%DestinationParameterOption.add_item("Sustain")
	%DestinationParameterOption.add_item("Decay")
	%DestinationParameterOption.add_item("Release")
	
func _populate_filter_Parameters():
	%DestinationParameterOption.add_item("Cutoff")
	%DestinationParameterOption.add_item("Resonance")
	%DestinationParameterOption.add_item("Gain")
	pass		

func get_source_name():
	return %SourceOption.get_item_text(%SourceOption.get_selected_id())
	
func get_destination_name():
	return %DestinationOption.get_item_text(%DestinationOption.get_selected_id())
	
func get_parameter_name():
	return %DestinationParameterOption.get_item_text(%DestinationParameterOption.get_selected_id())

func _on_destination_parameter_option_item_selected(index: int) -> void:
	if (index < 0):
		parameter_Selected.emit("")
	parameter_Selected.emit(%DestinationParameterOption.get_item_text(index))


func _on_delete_button_pressed() -> void:
	delete_pressed.emit(self)

func _on_source_option_item_selected(_index: int) -> void:
	%DestinationOption.clear()
	var selectedItmText = get_source_name()
	for itm in range (0, %SourceOption.item_count):
		var txt = %SourceOption.get_item_text(itm)
		if (txt != selectedItmText):
			%DestinationOption.add_item(txt)
	
