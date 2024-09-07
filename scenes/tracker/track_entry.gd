extends PanelContainer

# Expand the buffer to accommodate four hex digits: two for GainLabel and two for FXLabel
var hex_input_buffers = ["", "", "", ""]  # 4 slots for Gain and FX labels

# List of labels in the TrackEntry
var labels = []  # List of labels in the TrackEntry

# Track the currently focused label index (used for hex digits or other labels)
var current_focus_index = 0  # Declare it here for global access

# Key mappings for notes
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

var base_octave: int = 4
var default_note: String = "--"

func _ready():
	# Initialize the labels and focus visualization
	labels = [
		%NoteLabel,
		%GainLabelDigit1,  # First digit of GainLabel
		%GainLabelDigit2,  # Second digit of GainLabel
		%FXLabelDigit1,    # First digit of FXLabel
		%FXLabelDigit2     # Second digit of FXLabel
	]
	clear_focus_visualization()

func delete_note():
	%NoteLabel.text = default_note
	%GainLabelDigit1.text = '-'
	%GainLabelDigit2.text = '-'
	%FXLabelDigit1.text = '-'
	%FXLabelDigit2.text = '-'

# Set the initial focus visualization for this entry based on label index
func set_initial_focus(label_index: int):
	current_focus_index = label_index  # Track current label focus
	visualize_focus()

# Clear the focus visualization when this entry loses focus
func clear_focus_visualization():
	for label in labels:
		label.modulate = Color(1, 1, 1, 0.5)  # Dim labels

# Visualize focus for the currently focused label or hex digit
func visualize_focus():
	clear_focus_visualization()  # Clear focus from all labels
	labels[current_focus_index].modulate = Color(1, 1, 1, 1)  # Highlight the focused label

# Handle input based on the focused label or hex digit
func handle_input_for_focused_label(event: InputEventKey, label_index: int):
	current_focus_index = label_index

	if labels[current_focus_index] == %NoteLabel:
		# Handle note input for NoteLabel
		if white_keys.has(event.keycode) or black_keys.has(event.keycode):
			set_note_from_key(event.keycode)
	elif labels[current_focus_index] in [%GainLabelDigit1, %GainLabelDigit2, %FXLabelDigit1, %FXLabelDigit2]:
		handle_hex_digit_input(event)

# Handle input for hex digits and display it immediately
func handle_hex_digit_input(event: InputEventKey):
	var key_str = ""
	
	# Keycodes for digits 0-9
	if event.keycode >= KEY_0 and event.keycode <= KEY_9:
		key_str = str(event.keycode - KEY_0)
	# Keycodes for A-F
	elif event.keycode >= KEY_A and event.keycode <= KEY_F:
		key_str = String(char(event.keycode))

	# Check if the entered key is a valid hex digit
	if key_str in "0123456789ABCDEF":
		# Update the currently focused hex digit
		labels[current_focus_index].text = key_str
		
		# Store the input in the appropriate buffer index
		var buffer_index = current_focus_index - 1  # Adjust index to match hex_input_buffers
		hex_input_buffers[buffer_index] = key_str

		# Move focus to the next digit after input
		if current_focus_index < labels.size() - 1:
			current_focus_index += 1
			visualize_focus()

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

	%NoteLabel.text = note + str(octave)
