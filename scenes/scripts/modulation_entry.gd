extends HBoxContainer


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	_on_destination_option_item_selected(0)


# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass


func _on_destination_option_item_selected(index: int) -> void:
	var itemName = %DestinationOption.get_item_text(index);
	%DestinationParameterOption.clear()
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
	%DestinationParameterOption.add_item("Phase mod")
	pass
	
func _populate_lfo_Parameters():
	pass	

func _populate_envelope_Parameters():
	pass
	
func _populate_filter_Parameters():
	pass		
