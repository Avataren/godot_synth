extends PanelContainer

# External references for notes and input handling
var base_octave: int = 4
var default_note: String = "--"
var track_entries = []
var current_focus_index = 0  # Declare the current focus index variable

# List of labels in the TrackEntry
var labels = []

# Key mappings for notes and hex input
var white_keys = {
	KEY_Q: "C", KEY_W: "D", KEY_E: "E", KEY_R: "F", 
	KEY_T: "G", KEY_Y: "A", KEY_U: "B",  
	KEY_Z: "C", KEY_X: "D", KEY_C: "E", KEY_V: "F", 
	KEY_B: "G", KEY_N: "A", KEY_M: "B"
}

var black_keys = {
	KEY_2: "C#", KEY_3: "D#", KEY_5: "F#", 
	KEY_6: "G#", KEY_7: "A#", 
	KEY_S: "C#", KEY_D: "D#", KEY_G: "F#", 
	KEY_H: "G#", KEY_J: "A#"
}

func _ready():
	# Initialize the labels and focus visualization
	labels = [
		$TrackContainer/NoteLabel,
		$TrackContainer/GainLabel,
		$TrackContainer/FXLabel
	]
	clear_focus_visualization()

# Set the initial focus visualization for this entry based on label index
func set_initial_focus(label_index: int):
	current_focus_index = label_index  # Track current label focus
	visualize_focus()

# Clear the focus visualization when this entry loses focus
func clear_focus_visualization():
	for label in labels:
		label.modulate = Color(1, 1, 1, 0.5)  # Dim labels

# Visualize focus for the currently focused label
func visualize_focus():
	clear_focus_visualization()  # Clear focus from all labels
	labels[current_focus_index].modulate = Color(1, 1, 1, 1)  # Highlight the focused label

# Handle input based on the focused label
func handle_input_for_focused_label(event: InputEventKey, label_index: int):
	current_focus_index = label_index
	if labels[current_focus_index] == $TrackContainer/NoteLabel:
		# Handle note input for NoteLabel
		if white_keys.has(event.keycode) or black_keys.has(event.keycode):
			set_note_from_key(event.keycode)
	elif labels[current_focus_index] == $TrackContainer/GainLabel or labels[current_focus_index] == $TrackContainer/FXLabel:
		# Handle hexadecimal input for GainLabel and FXLabel
		var key_str = str(event.unicode).to_upper()  # Ensure uppercase hex values
		if key_str in "0123456789ABCDEF":
			set_hex_value(key_str)

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
		if keycode in [KEY_S, KEY_D, KEY_G, KEY_H, KEY_J]:
			octave -= 1

	$TrackContainer/NoteLabel.text = note + str(octave)

# Set a hexadecimal value for GainLabel and FXLabel
func set_hex_value(hex_value: String):
	labels[current_focus_index].text = hex_value

# Delete the current note
func delete_note():
	$TrackContainer/NoteLabel.text = default_note
