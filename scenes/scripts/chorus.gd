extends PanelContainer

signal delay_changed
signal depth_changed
signal frequency_changed
signal feedback_changed
signal mix_changed
signal enabled_changed
signal cutoff_changed

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	delay_changed.emit(%DelayKnob.current_value)
	depth_changed.emit(%DepthKnob.current_value)
	frequency_changed.emit(%FrequencyKnob.current_value)
	feedback_changed.emit(%FeedbackKnob.current_value)
	mix_changed.emit(%WetKnob.current_value)

func _on_delay_knob_value_changed(val) -> void:
	delay_changed.emit(val)

func _on_depth_knob_value_changed(val) -> void:
	depth_changed.emit(val)

func _on_frequency_knob_value_changed(val) -> void:
	frequency_changed.emit(val)

func _on_feedback_knob_value_changed(val) -> void:
	feedback_changed.emit(val)

func _on_wet_knob_value_changed(val) -> void:
	mix_changed.emit(val)

func _on_enabled_check_box_toggled(toggled_on: bool) -> void:
	enabled_changed.emit(toggled_on)

func _on_filter_knob_value_changed(val) -> void:
	cutoff_changed.emit(val)
