extends Control
class_name Knob

# Exported properties
@export var min_value: float = 0.0
@export var max_value: float = 100.0
@export var start_angle: float = 220.0
@export var end_angle: float = 500.0
@export var step: float = 1.0
@export var angle_offset: float = -90.0
@export var sensitivity: float = 0.5
@export var label_unit_scale: = 1.0
@export var label_unit: = ""
@export var title: String = "knob":
	set(value):
		title = value
		_update_title()
@export var nonlinear_factor: float = 1.0 # Factor for non-linear adjustment; 1.0 means linear

# Internal state
@export var current_value: float = min_value:
	set(value):
		current_value = roundf(value * 1000.0) / 1000.0
		if (current_value > max_value):
			current_value = max_value
		if (current_value < min_value):
			current_value = min_value
		_update_current_value()

var previous_value: float = current_value
var mouse_hovered: bool = false
var mouse_drag: bool = false
var mouse_drag_start_value: float = min_value
var accumulated_value: float = 0.0

signal value_changed

func _ready():
	_update_pointer_rotation()
	%TitleLabel.text = title
	update_value_label()

func update_value_label():
	if (label_unit_scale < 0.0001):
		label_unit_scale = 1.0
	if (!label_unit):
		label_unit = ""
	%ValueLabel.text = str(current_value * label_unit_scale) + label_unit

func _update_title():
	if Engine.is_editor_hint():
		%TitleLabel.text = title
		
func _update_current_value():
	if Engine.is_editor_hint():
		#%ValueLabel.text = str(current_value)
		update_value_label()
		_update_pointer_rotation()
		
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
	
	# Apply non-linear mapping
	var new_value = clamp(mouse_drag_start_value + accumulated_value, min_value, max_value)
	var normalized_value = (new_value - min_value) / (max_value - min_value)
	normalized_value = pow(normalized_value, 1.0 / nonlinear_factor)
	new_value = min_value + normalized_value * (max_value - min_value)
		
	current_value = round(new_value / step) * step
	if previous_value != current_value:
		#%ValueLabel.text = str(current_value)
		update_value_label()
		previous_value = current_value
		value_changed.emit(current_value)
	
	_update_pointer_rotation()

func _on_start_drag() -> void:
	mouse_drag = true
	# Calculate the normalized value according to the current value and nonlinear factor
	var normalized_value = (current_value - min_value) / (max_value - min_value)
	if nonlinear_factor != 1.0:
		normalized_value = pow(normalized_value, nonlinear_factor)
	mouse_drag_start_value = min_value + normalized_value * (max_value - min_value)
	accumulated_value = 0.0

func _on_stop_drag() -> void:
	mouse_drag = false

func _on_mouse_entered() -> void:
	mouse_hovered = true

func _on_mouse_exited() -> void:
	mouse_hovered = false

func _update_pointer_rotation() -> void:
	# Map the current value to the corresponding angle
	# var angle_deg = _value_to_angle(current_value)
	#%Pointer.rotation_degrees = angle_deg
	
	# Calculate normalized progress correctly, considering min_value could be negative
	var normalized_value = (current_value - min_value) / (max_value - min_value)
	%ColorRect.material.set("shader_parameter/progress", normalized_value)

#func _value_to_angle(value: float) -> float:
	# Convert a value to an angle between `start_angle` and `end_angle`
#	var normalized_value = (value - min_value) / (max_value - min_value)
	#normalized_value = pow(normalized_value, nonlinear_factor)
	#value = min_value + normalized_value * (max_value - min_value)
	#return lerp(start_angle, end_angle, (value - min_value) / (max_value - min_value))
