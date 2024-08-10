extends Control

# Exported properties
@export var min_value: float = 0.0
@export var max_value: float = 100.0
@export var start_angle: float = 220.0
@export var end_angle: float = 500.0
@export var step: float = 1.0
@export var angle_offset: float = -90.0
@export var sensitivity: float = 0.5
@export var title = "knob"

# Internal state
@export var current_value: float = min_value
var previous_value: float = current_value
var mouse_hovered: bool = false
var mouse_drag: bool = false
var mouse_drag_start_value: float = min_value
var accumulated_value: float = 0.0

signal value_changed

func _ready():
	_update_pointer_rotation()
	%TitleLabel.text = title
	%ValueLabel.text = str(current_value)
	
func _unhandled_input(event: InputEvent) -> void:
	if event is InputEventMouseButton and event.button_index == MOUSE_BUTTON_LEFT:
		if event.pressed and mouse_hovered:
			_on_start_drag()
		elif mouse_drag and not event.pressed:
			_on_stop_drag()
	elif event is InputEventMouseMotion and mouse_drag:
		_process_motion(event.relative)

func _process_motion(relative: Vector2) -> void:
	# Adjust the knob value based on vertical mouse movement
	accumulated_value += -relative.y * sensitivity * (max_value - min_value) / 100.0
	current_value = clamp(mouse_drag_start_value + accumulated_value, min_value, max_value)
	current_value = round(current_value / step) * step
	if (previous_value != current_value):
		%ValueLabel.text = str(current_value)
		previous_value = current_value
		value_changed.emit(current_value)
	
	print(current_value)
	_update_pointer_rotation()

func _on_start_drag() -> void:
	mouse_drag = true
	mouse_drag_start_value = current_value
	accumulated_value = 0.0

func _on_stop_drag() -> void:
	mouse_drag = false

func _on_mouse_entered() -> void:
	mouse_hovered = true

func _on_mouse_exited() -> void:
	mouse_hovered = false

func _update_pointer_rotation() -> void:
	# Map the current value to the corresponding angle
	var angle_deg = _value_to_angle(current_value)
	%Pointer.rotation_degrees = angle_deg

func _value_to_angle(value: float) -> float:
	# Convert a value to an angle between `start_angle` and `end_angle`
	return lerp(start_angle, end_angle, (value - min_value) / (max_value - min_value))
