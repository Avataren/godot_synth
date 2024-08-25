extends PanelContainer

signal input_gain_changed
signal output_gain_changed
signal mix_changed
signal cutoff_changed
signal feedback_changed
signal bias_changed
signal enabled_changed


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	input_gain_changed.emit(%InputGainKnob.current_value)
	output_gain_changed.emit(%OutputGainKnob.current_value)
	mix_changed.emit(%MixKnob.current_value)
	cutoff_changed.emit(%CutoffKnob.current_value)
	feedback_changed.emit(%FeedbackKnob.current_value)
	bias_changed.emit(%BiasKnob.current_value)
	enabled_changed.emit(%EnabledCheckBox.pressed)
	

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _process(delta: float) -> void:
	pass


func _on_bias_knob_value_changed(val) -> void:
	bias_changed.emit(val)

func _on_input_gain_knob_value_changed(val) -> void:
	input_gain_changed.emit(val)

func _on_output_gain_knob_value_changed(val) -> void:
	output_gain_changed.emit(val)

func _on_mix_knob_value_changed(val) -> void:
	mix_changed.emit(val)

func _on_cutoff_knob_value_changed(val) -> void:
	cutoff_changed.emit(val)

func _on_feedback_knob_value_changed(val) -> void:
	feedback_changed.emit(val)

func _on_enabled_check_box_toggled(toggled_on: bool) -> void:
	enabled_changed.emit(toggled_on)
