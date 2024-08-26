extends Control

signal cutoff_changed
signal resonance_changed
signal drive_changed
signal filter_type_changed
# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	%CutoffKnob.value_changed.connect(_on_cutoff_changed)
	%FrequencyKnob.value_changed.connect(_on_resonance_changed)
	%DriveKnob.value_changed.connect(_on_drive_changed)
	_connect_filter_change()
	
func _on_cutoff_changed(val) -> void:
	cutoff_changed.emit(val)
	
func _on_resonance_changed(val) -> void:
	resonance_changed.emit(val)
	
func _on_drive_changed(val) -> void:
	drive_changed.emit(val)
	
func _connect_filter_change():
	for child in %GridContainer.get_children():
		if (child is Button):
			child.pressed.connect(_filter_selected.bind(child.text))
			
func _filter_selected(filter):
	filter_type_changed.emit(filter)
