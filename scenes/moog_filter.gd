extends Control

signal cutoff_changed
signal resonance_changed
# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	%CutoffKnob.value_changed.connect(_on_cutoff_changed)
	%FrequencyNob.value_changed.connect(_on_resonance_changed)

func _on_cutoff_changed(val) -> void:
	cutoff_changed.emit(val)
	
func _on_resonance_changed(val) -> void:
	resonance_changed.emit(val)
	
# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass
