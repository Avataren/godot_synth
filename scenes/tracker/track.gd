extends VBoxContainer

@onready var container = $"."
@onready var track_entry_scene = preload("res://scenes/tracker/track_entry.tscn")

var track_entries = []
var current_track_index = 0

func _ready() -> void:
	clear_container()
	fill_tracks(48)

	# Set focus to the first track entry
	track_entries[0].grab_focus()
	track_entries[0].visualize_focus()

# Clear container and tracks
func clear_container():
	for child in container.get_children():
		child.queue_free()
	track_entries.clear()

# Fill track container with track entries
func fill_tracks(num:int):
	for i in range(num):
		var track_entry = track_entry_scene.instantiate()
		track_entries.append(track_entry)
		track_entry.track_entries = track_entries
		container.add_child(track_entry)

		# Connect signals for out-of-bounds focus navigation using Callable
		track_entry.connect("focus_right_out_of_bounds", Callable(self, "_on_focus_right_out_of_bounds"))
		track_entry.connect("focus_left_out_of_bounds", Callable(self, "_on_focus_left_out_of_bounds"))

# Move to the next track when focus goes out of bounds to the right
func _on_focus_right_out_of_bounds():
	if current_track_index < track_entries.size() - 1:
		# Clear focus visualization on the current track
		track_entries[current_track_index].clear_focus_visualization()

		# Move to the next track and set focus
		current_track_index += 1
		track_entries[current_track_index].grab_focus()
		track_entries[current_track_index].set_focus_on_label(0)  # Start with NoteLabel

# Move to the previous track when focus goes out of bounds to the left
func _on_focus_left_out_of_bounds():
	if current_track_index > 0:
		# Clear focus visualization on the current track
		track_entries[current_track_index].clear_focus_visualization()

		# Move to the previous track and set focus
		current_track_index -= 1
		track_entries[current_track_index].grab_focus()
		track_entries[current_track_index].set_focus_on_label(2)  # Start with FXLabel (last label)
