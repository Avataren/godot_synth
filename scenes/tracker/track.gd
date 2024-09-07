extends VBoxContainer

@onready var container = $"."
@onready var track_entry_scene = preload("res://scenes/tracker/track_entry.tscn")

var track_entries = []
var current_track_index = 0
var step_size: int = 1  # Step size for movement
var current_label_focus_index = 0  # Focus between NoteLabel, GainLabel, and FXLabel

func _ready() -> void:
	clear_container()
	fill_tracks(48)
	track_entries[current_track_index].set_initial_focus(current_label_focus_index)

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
		track_entry.track_entries = track_entries
		container.add_child(track_entry)

# Input event handling at the track level
func _input(event: InputEvent):
	if event is InputEventKey and event.pressed:
		match event.keycode:
			KEY_RIGHT:
				move_focus_right()
			KEY_LEFT:
				move_focus_left()
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
				handle_input_for_current_track(event)

# Move focus right between labels in the current track_entry
func move_focus_right():
	if current_label_focus_index < 2:  # We have 3 labels, so index 0-2
		current_label_focus_index += 1
		track_entries[current_track_index].set_initial_focus(current_label_focus_index)

# Move focus left between labels in the current track_entry
func move_focus_left():
	if current_label_focus_index > 0:
		current_label_focus_index -= 1
		track_entries[current_track_index].set_initial_focus(current_label_focus_index)

# Move focus up between track entries
func move_focus_up(step: int = 1):
	if current_track_index > 0:
		track_entries[current_track_index].clear_focus_visualization()  # Clear current entry's visualization
		current_track_index -= step  # Move to the previous track entry
		track_entries[current_track_index].set_initial_focus(current_label_focus_index)  # Set focus on the new entry at the same label

# Move focus down between track entries
func move_focus_down(step: int = 1):
	if current_track_index < track_entries.size() - 1:
		track_entries[current_track_index].clear_focus_visualization()  # Clear current entry's visualization
		current_track_index += step  # Move to the next track entry
		track_entries[current_track_index].set_initial_focus(current_label_focus_index)  # Set focus on the new entry at the same label

# Handle input specific to the current track_entry's focused label
func handle_input_for_current_track(event: InputEventKey):
	track_entries[current_track_index].handle_input_for_focused_label(event, current_label_focus_index)
	# After input, move focus down if the current label is the NoteLabel and a note was entered
	if current_label_focus_index == 0 and event.pressed:
		move_focus_down(step_size)

# Delete note in the current track_entry
func delete_note():
	track_entries[current_track_index].delete_note()
