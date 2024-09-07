extends VBoxContainer

@onready var container = $"."
@onready var track_entry_scene = preload("res://scenes/tracker/track_entry.tscn")

var track_entries = []
var current_track_index = 0
var step_size: int = 1  # Step size for movement
var current_label_focus_index = 0  # Focus between NoteLabel, GainLabel, and FXLabel

# Define valid hex key mappings (0-9 and A-F)
var hex_keys = [
	KEY_0, KEY_1, KEY_2, KEY_3, KEY_4, KEY_5, 
	KEY_6, KEY_7, KEY_8, KEY_9,
	KEY_A, KEY_B, KEY_C, KEY_D, KEY_E, KEY_F
]

static var first_init = false
func _ready() -> void:
	clear_container()
	fill_tracks(48)
	_on_focus_exited()
	# Set focus on the first TrackEntry with the first label initially
	if (!first_init):
		first_init = true
		track_entries[0].set_initial_focus(0)
		grab_focus()

# Clear container and fill tracks
func clear_container():
	for child in container.get_children():
		child.queue_free()
	track_entries.clear()

# Add track entries to the container
func fill_tracks(num: int):
	for i in range(num):
		var track_entry = track_entry_scene.instantiate()
		track_entries.append(track_entry)
		container.add_child(track_entry)

# Input event handling at the track level
func _input(event: InputEvent):
	if !has_focus():
		return
		
	if event is InputEventKey and event.pressed:
		match event.keycode:
			KEY_RIGHT:
				move_focus_right()
				accept_event()
			KEY_LEFT:
				move_focus_left()
				accept_event()
			KEY_UP:
				move_focus_up(step_size)
			KEY_DOWN:
				move_focus_down(step_size)
			KEY_PAGEUP:
				move_focus_up(16)
			KEY_PAGEDOWN:
				move_focus_down(16)
			KEY_DELETE:
				delete_note()
				move_focus_down(step_size)
			_:
				# Delegate input handling to the currently focused track entry
				handle_input_for_current_track(event)

# Move focus right between labels in the current track_entry
func move_focus_right():
	if current_label_focus_index < 4:  # 0-4 for the 5 labels
		current_label_focus_index += 1
		print_debug_info()  # Debug output
		track_entries[current_track_index].set_initial_focus(current_label_focus_index)

# Move focus left between labels in the current track_entry
func move_focus_left():
	if current_label_focus_index > 0:
		current_label_focus_index -= 1
		print_debug_info()  # Debug output
		track_entries[current_track_index].set_initial_focus(current_label_focus_index)

# Move focus up between track entries
func move_focus_up(step: int = 1):
	# Clear the current entry's visualization
	track_entries[current_track_index].clear_focus_visualization()
	# Move up by the step size
	current_track_index -= step
	# If the index goes out of bounds, set it to the first entry (index 0)
	if current_track_index < 0:
		current_track_index = 0
	print_debug_info()  # Debug output to track current index
	track_entries[current_track_index].set_initial_focus(current_label_focus_index)  # Set focus on the new entry at the same label

# Move focus down between track entries
func move_focus_down(step: int = 1):
	if current_track_index < track_entries.size() - 1:
		track_entries[current_track_index].clear_focus_visualization()  # Clear current entry's visualization
		current_track_index += step  # Move to the next track entry
		print_debug_info()  # Debug output
		track_entries[current_track_index].set_initial_focus(current_label_focus_index)  # Set focus on the new entry at the same label

# Handle input specific to the current track_entry's focused label
func handle_input_for_current_track(event: InputEventKey):
	track_entries[current_track_index].handle_input_for_focused_label(event, current_label_focus_index)

	# Move to the next entry after input for notes
	if current_label_focus_index == 0 and event.pressed:
		move_focus_down(step_size)
	# Check for valid hex input and move focus right for hex labels
	elif current_label_focus_index > 0 and current_label_focus_index < 5:
		if event.keycode in hex_keys:
			move_focus_right()

# Delete note in the current track_entry
func delete_note():
	track_entries[current_track_index].delete_note()

# Print debugging information to check focus states
func print_debug_info():
	print("Current Track Index: ", current_track_index)
	print("Current Label Focus Index: ", current_label_focus_index)

func _on_focus_entered() -> void:
	modulate =  Color(1, 1, 1, 1.0)

func _on_focus_exited() -> void:
	modulate =  Color(0.6, 0.6, 0.6, 1.0)
