extends PanelContainer

# Assuming base_octave is provided from the tracker
var base_octave: int = 4  # Set dynamically later
var step_size: int = 1  # Default step size for regular movements
var page_step_size: int = 16  # Default step size for Page Up/Down movements
var default_note: String = "--"  # Default text when note is deleted

# Define your key-to-note mappings
var white_keys = {
	KEY_Q: "C", KEY_W: "D", KEY_E: "E", KEY_R: "F", KEY_T: "G", KEY_Y: "A", KEY_U: "B",  # Upper octave
	KEY_Z: "C", KEY_X: "D", KEY_C: "E", KEY_V: "F", KEY_B: "G", KEY_N: "A", KEY_M: "B"   # Lower octave
}

var black_keys = {
	KEY_2: "C#", KEY_3: "D#", KEY_5: "F#", KEY_6: "G#", KEY_7: "A#",  # Upper octave
	KEY_S: "C#", KEY_D: "D#", KEY_G: "F#", KEY_H: "G#", KEY_J: "A#", KEY_L: "B#", KEY_O: "D#"  # Lower octave
}

func _ready():
	set_focus_mode(Control.FOCUS_ALL)
	grab_focus()  # Ensure the TrackEntry can get focus.

	# Connect focus signals using Callable
	connect("focus_entered", Callable(self, "_on_focus_entered"))
	connect("focus_exited", Callable(self, "_on_focus_exited"))

# Capture input from the keyboard, but only respond if this TrackEntry has focus
func _input(event: InputEvent):
	if event is InputEventKey and event.pressed and has_focus():
		match event.keycode:
			# Handle white and black key presses for upper and lower octaves
			KEY_Q, KEY_W, KEY_E, KEY_R, KEY_T, KEY_Y, KEY_U, KEY_2, KEY_3, KEY_5, KEY_6, KEY_7,	KEY_Z, KEY_X, KEY_C, KEY_V, KEY_B, KEY_N, KEY_M, KEY_S, KEY_D, KEY_G, KEY_H, KEY_J, KEY_L, KEY_O:
				set_note_from_key(event.keycode)
				await get_tree().process_frame  # Wait until the next frame
				move_focus_down(step_size)  # Move to the next entry based on step size
			# Handle DEL key to delete note and move focus
			KEY_DELETE:
				delete_note()
				await get_tree().process_frame  # Wait until the next frame
				move_focus_down(step_size)
			# Handle arrow key navigation
			KEY_UP:
				await get_tree().process_frame  # Wait until the next frame
				move_focus_up()
			KEY_DOWN:
				await get_tree().process_frame  # Wait until the next frame
				move_focus_down()
			# Handle Page Up and Page Down for skipping 16 entries, with boundary checks
			KEY_PAGEUP:
				await get_tree().process_frame  # Wait until the next frame
				move_focus_up(page_step_size)
			KEY_PAGEDOWN:
				await get_tree().process_frame  # Wait until the next frame
				move_focus_down(page_step_size)

# Map keycode to note and update the NoteLabel
func set_note_from_key(keycode):
	var note = ""
	var octave = base_octave  # Default to the base octave

	# Handle upper octave keys
	if white_keys.has(keycode):
		note = white_keys[keycode]
		if keycode in [KEY_Z, KEY_X, KEY_C, KEY_V, KEY_B, KEY_N, KEY_M]:  # Lower octave keys
			octave -= 1  # Shift down one octave for lower row

	elif black_keys.has(keycode):
		note = black_keys[keycode]
		if keycode in [KEY_S, KEY_D, KEY_G, KEY_H, KEY_J, KEY_L, KEY_O]:  # Lower octave keys
			octave -= 1  # Shift down one octave for lower row

	# Append the octave to the note
	var full_note = note + str(octave)

	# Update the NoteLabel text
	$TrackContainer/NoteLabel.text = full_note

# Handle deleting the note and replacing it with default text
func delete_note():
	$TrackContainer/NoteLabel.text = default_note

# Handle moving focus to the previous TrackEntry, with boundary check
func move_focus_up(step: int = 1):
	var parent = get_parent()
	var index = parent.get_children().find(self)

	if index - step < 0:
		print("Move up: Reached top, focusing on first entry.")
		var first_child = parent.get_child(0)
		first_child.set_focus_mode(Control.FOCUS_ALL)  # Ensure the first child can receive focus
		first_child.grab_focus()
	else:
		print("Move up: Moving up by step: ", step, " from index: ", index)
		var prev_sibling = parent.get_child(index - step)
		prev_sibling.set_focus_mode(Control.FOCUS_ALL)  # Ensure the previous sibling can receive focus
		prev_sibling.grab_focus()

# Handle moving focus to the next TrackEntry, with boundary check
func move_focus_down(step: int = 1):
	var parent = get_parent()
	var index = parent.get_children().find(self)
	var total_children = parent.get_child_count()

	if index + step >= total_children:
		print("Move down: Reached bottom, focusing on last entry.")
		var last_child = parent.get_child(total_children - 1)
		last_child.set_focus_mode(Control.FOCUS_ALL)  # Ensure the last child can receive focus
		last_child.grab_focus()

		# Reset the index so Page Up can work properly
		index = total_children - 1
	else:
		print("Move down: Moving down by step: ", step, " from index: ", index)
		var next_sibling = parent.get_child(index + step)
		next_sibling.set_focus_mode(Control.FOCUS_ALL)  # Ensure the next sibling can receive focus
		next_sibling.grab_focus()

# Called when focus is gained to highlight the entry
func _on_focus_entered():
	modulate = Color(1, 1, 1, 1)  # Set to a highlighted color (e.g., white)

# Called when focus is lost to remove the highlight
func _on_focus_exited():
	modulate = Color(1, 1, 1, 0.5)  # Set to a dimmed color (or default)
