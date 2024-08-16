extends Control

signal freq_value_changed(value)
signal gain_value_changed(value)
signal bias_value_changed(value)
signal waveform_changed(value)
signal abs_value_changed(value)
signal adsr_toggled(value)

func _ready() -> void:
	set_meta("isLFO", true)
	freq_value_changed.emit(%FreqKnob.current_value) 
	gain_value_changed.emit(%GainKnob.current_value)
	bias_value_changed.emit(%BiasKnob.current_value)
	waveform_changed.emit(%WaveformOptions.get_item_text(%WaveformOptions.get_selected_id()))
	abs_value_changed.emit(%AbsCheckButton.button_pressed)
	
	%FreqKnob.value_changed.connect(func(value): freq_value_changed.emit (value))
	%GainKnob.value_changed.connect(func(value): gain_value_changed.emit (value))
	%BiasKnob.value_changed.connect(func(value): bias_value_changed.emit (value))
	%WaveformOptions.item_selected.connect(func(value): waveform_changed.emit(%WaveformOptions.get_item_text(value)))
	%AbsCheckButton.toggled.connect(func(value): abs_value_changed.emit(value))
	%ADSR_Envelope.visible = false
	adsr_toggled.emit(false)

func _on_enable_envelope_toggled(toggled_on: bool) -> void:
	%ADSR_Envelope.visible = toggled_on
	adsr_toggled.emit(toggled_on)
