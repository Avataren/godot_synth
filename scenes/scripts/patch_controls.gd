extends PanelContainer

signal gain_changed
signal portamento_changed

func _ready() -> void:
	portamento_changed.emit(%PortamentoKnob.current_value)
	gain_changed.emit(%GainKnob.current_value)

func _on_portamento_knob_value_changed(val) -> void:
	portamento_changed.emit(val)


func _on_gain_knob_value_changed(val) -> void:
	gain_changed.emit(val)
