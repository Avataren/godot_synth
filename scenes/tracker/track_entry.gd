extends PanelContainer

# Define signals for boundary navigation
signal focus_right_out_of_bounds
signal focus_left_out_of_bounds

# External references
var base_octave: int = 4  # Set dynamically later
var step_size: int = 1  # Default step size for regular movements
var page_step_size: int = 16  # Default step size for Page Up/Down movements
var default_note: String = "--"  # Default text when note is deleted
var track_entries = []  # This will be passed from the parent

# Define key-to-note mappings for white and black keys
var white_keys = {
	KEY_Q: "C", KEY_W: "D", KEY_E: "E", KEY_R: "F", KEY_T: "G", KEY_Y: "A", KEY_U: "B",  # Upper octave
	KEY_Z: "C", KEY_X: "D", KEY_C: "E", KEY_V: "F", KEY_B: "G", KEY_N: "A", KEY_M: "B"   # Lower octave
}

var black_keys = {
	KEY_2: "C#", KEY_3: "D#", KEY_5: "F#", KEY_6: "G#", KEY_7: "A#",  # Upper octave
	KEY_S: "C#", KEY_D: "D#", KEY_G: "F#", KEY_H: "G#", KEY_J: "A#", KEY_L: "B#", KEY_O: "D#"  # Lower octave
}

# List of labels for focus navigation
var labels = []
var current_focus_index = 0  # Start with NoteLabel focused

func _ready():
	set_focus_mode(Control.FOCUS_ALL)
	grab_focus()  # Ensure the TrackEntry can get focus.

	# Store references to labels in the HBoxContainer
	labels = [
		$TrackContainer/NoteLabel,
		$TrackContainer/GainLabel,
		$TrackContainer/FXLabel
	]

	clear_focus_visualization()

# Clear the focus visualization for all labels
func clear_focus_visualization():
	for label in labels:
		label.modulate = Color(1, 1, 1, 0.5)  # Dim all labels

# Visualize which label currently has focus
func visualize_focus():
	clear_focus_visualization()
	labels[current_focus_index].modulate = Color(1, 1, 1, 1)  # Highlight the label with focus

# Move focus to the next label when pressing right
func move_focus_right():
	if current_focus_index < labels.size() - 1:
		current_focus_index += 1
		visualize_focus()
	else:
		emit_signal("focus_right_out_of_bounds")  # Signal to move to the next track

# Move focus to the previous label when pressing left
func move_focus_left():
	if current_focus_index > 0:
		current_focus_index -= 1
		visualize_focus()
	else:
		emit_signal("focus_left_out_of_bounds")  # Signal to move to the previous track

# Handle input from the keyboard
func _input(event: InputEvent):
	if event is InputEventKey and event.pressed and has_focus():
		match event.keycode:
			KEY_RIGHT:
				move_focus_right()
			KEY_LEFT:
				move_focus_left()
			KEY_UP:
				await get_tree().process_frame
				move_focus_up()
			KEY_DOWN:
				await get_tree().process_frame
				move_focus_down()
			KEY_PAGEUP:
				await get_tree().process_frame
				move_focus_up(page_step_size)
			KEY_PAGEDOWN:
				await get_tree().process_frame
				move_focus_down(page_step_size)
			KEY_DELETE:
				delete_note()
				await get_tree().process_frame
				move_focus_down(step_size)
			# Handle white and black key presses for note input
			_:
				if white_keys.has(event.keycode) or black_keys.has(event.keycode):
					set_note_from_key(event.keycode)
					await get_tree().process_frame
					move_focus_down(step_size)

# Move focus to the previous TrackEntry (above), using track_entries array
func move_focus_up(step: int = 1):
	var index = track_entries.find(self)  # Find this TrackEntry's index in track_entries array

	if index - step < 0:
		# If trying to move beyond the first entry, focus remains on the first entry
		var first_child = track_entries[0]
		first_child.grab_focus()  # Focus on the first entry
		first_child.set_focus_on_label(current_focus_index)  # Keep focus on the same label type
	else:
		# Move to the previous sibling in the track_entries array
		var prev_sibling = track_entries[index - step]
		prev_sibling.grab_focus()  # Set focus on the previous entry
		prev_sibling.set_focus_on_label(current_focus_index)  # Keep focus on the same label type

	# Clear focus visualization from the current entry
	clear_focus_visualization()

# Move focus to the next TrackEntry (below), using track_entries array
func move_focus_down(step: int = 1):
	var index = track_entries.find(self)  # Find this TrackEntry's index in track_entries array

	if index + step >= track_entries.size():
		# If trying to move beyond the last entry, focus remains on the last entry
		var last_child = track_entries[track_entries.size() - 1]
		last_child.grab_focus()  # Focus on the last entry
		last_child.set_focus_on_label(current_focus_index)  # Keep focus on the same label type
	else:
		# Move to the next sibling in the track_entries array
		var next_sibling = track_entries[index + step]
		next_sibling.grab_focus()  # Set focus on the next entry
		next_sibling.set_focus_on_label(current_focus_index)  # Keep focus on the same label type

	# Clear focus visualization from the current entry
	clear_focus_visualization()

# Map keycode to note and update the NoteLabel
func set_note_from_key(keycode):
	var note = ""
	var octave = base_octave

	if white_keys.has(keycode):
		note = white_keys[keycode]
		if keycode in [KEY_Z, KEY_X, KEY_C, KEY_V, KEY_B, KEY_N, KEY_M]:
			octave -= 1
	elif black_keys.has(keycode):
		note = black_keys[keycode]
		if keycode in [KEY_S, KEY_D, KEY_G, KEY_H, KEY_J, KEY_L, KEY_O]:
			octave -= 1

	$TrackContainer/NoteLabel.text = note + str(octave)

# Handle deleting the note and replacing it with default text
func delete_note():
	$TrackContainer/NoteLabel.text = default_note

# Set the focus on the label based on the index passed from another track
func set_focus_on_label(focus_index: int):
	current_focus_index = focus_index
	visualize_focus()
