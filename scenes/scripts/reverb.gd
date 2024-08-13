extends PanelContainer

signal damp_changed
signal dry_changed
signal wet_changed
signal room_changed
signal width_changed
signal enabled_changed

func _on_damp_knob_value_changed(val:float) -> void:
	damp_changed.emit(val)

func _on_dry_knob_value_changed(val: float) -> void:
	dry_changed.emit(val)

func _on_wet_knob_value_changed(val: float) -> void:
	wet_changed.emit(val)

func _on_room_knob_value_changed(val: float) -> void:
	room_changed.emit(val)
	
func _on_width_knob_value_changed(val: float) -> void:
	width_changed.emit(val)

func _on_check_box_toggled(toggled_on: bool) -> void:
	enabled_changed.emit(toggled_on)
