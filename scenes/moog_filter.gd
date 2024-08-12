extends Control

signal cutoff_changed
signal resonance_changed
signal drive_changed
# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	%CutoffKnob.value_changed.connect(_on_cutoff_changed)
	%FrequencyKnob.value_changed.connect(_on_resonance_changed)
	%DriveKnob.value_changed.connect(_on_drive_changed)
	
func _on_cutoff_changed(val) -> void:
	cutoff_changed.emit(val)
	
func _on_resonance_changed(val) -> void:
	resonance_changed.emit(val)
	
func _on_drive_changed(val) -> void:
	drive_changed.emit(val)
