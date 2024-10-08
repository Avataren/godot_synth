extends PanelContainer

signal delay_changed
signal feedback_changed
signal wetmix_changed
signal delay_enabled
# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	delay_changed.emit(%DelayKnob.current_value)
	feedback_changed.emit(%FeedbackKnob.current_value)
	wetmix_changed.emit(%WetMixKnob.current_value)

func _on_delay_knob_value_changed(val: int) -> void:
	delay_changed.emit(val)

func _on_feedback_knob_value_changed(val: float) -> void:
	feedback_changed.emit(val)

func _on_wet_mix_knob_value_changed(val: float) -> void:
	wetmix_changed.emit(val)

func _on_check_box_toggled(toggled_on: bool) -> void:
	delay_enabled.emit(toggled_on)
