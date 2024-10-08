extends PanelContainer

signal gain_changed
signal offset_changed
signal enabled_changed
signal noisetype_changed
signal slope_changed

func _ready() -> void:
	gain_changed.emit(%GainKnob.current_value)
	offset_changed.emit(%DCOffsetKnob.current_value)
	
func _on_gain_knob_value_changed(val) -> void:
	gain_changed.emit(val)

func _on_dc_offset_knob_value_changed(val) -> void:
	offset_changed.emit(val)

func _on_enabled_check_box_toggled(toggled_on: bool) -> void:
	enabled_changed.emit(toggled_on)

func _on_white_noise_button_pressed() -> void:
	noisetype_changed.emit("white")

func _on_pink_noise_button_pressed() -> void:
	noisetype_changed.emit("pink")

func _on_brownian_noise_button_pressed() -> void:
	noisetype_changed.emit("brown")

func _on_slope_knob_value_changed(val) -> void:
	slope_changed.emit(val)
