extends Control

@export var min_value := 0.0
@export var max_value := 100.0
@export var start_angle := 20.0
@export var end_angle := 340.0
@export var step := 1.0
@export var angle_offset := -90.0

var current_value := min_value
var is_dragging := false

func _ready():
	# Initialize the knob pointer rotation based on current value
	# _update_pointer_rotation()
	pass

func _draw():
	# Drawing the knob background (example)
	draw_circle(Vector2(0, 0), 50, Color(0.5, 0.5, 0.5))

	# Calculate the angle based on the current value
	var angle_deg = lerp(start_angle, end_angle, (current_value - min_value) / (max_value - min_value))

	# Convert to radians and compute the handle position
	var angle_rad = deg_to_rad(angle_deg + angle_offset)
	var handle_pos = Vector2(cos(angle_rad), sin(angle_rad)) * 40

	# Drawing the handle (example)
	draw_circle(handle_pos, 10, Color(0.8, 0.8, 0.8))

func _input_event(event):
	if event is InputEventMouseButton and event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT:
			# Start dragging
			is_dragging = true

	elif event is InputEventMouseMotion and is_dragging:
		# Update the angle and value based on mouse position
		var mouse_pos = get_local_mouse_position()
		var angle = Vector2(1, 0).angle_to_point(mouse_pos)
		var angle_deg = rad_to_deg(angle) - angle_offset

		# Normalize the angle and update the value
		angle_deg = fmod(angle_deg + 360.0, 360.0)
		_update_value_from_angle(angle_deg)

	elif event is InputEventMouseButton and not event.pressed:
		if event.button_index == MOUSE_BUTTON_LEFT:
			# Stop dragging
			is_dragging = false

func _update_value_from_angle(angle_deg):
	var adjusted_start_angle = fmod(start_angle, 360.0)
	var adjusted_end_angle = fmod(end_angle, 360.0)

	var angle_range = fmod(adjusted_end_angle - adjusted_start_angle + 360.0, 360.0)
	var norm_angle = fmod(angle_deg - adjusted_start_angle + 360.0, 360.0) / angle_range

	current_value = lerp(min_value, max_value, norm_angle)
	#_update_pointer_rotation()

#func _update_pointer_rotation():
	
