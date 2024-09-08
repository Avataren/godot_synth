extends Control

@onready var num_scene = preload("res://scenes/tracker/num_entry.tscn")
# Called when the node enters the scene tree for the first time.

var tracks = []
var current_track := 0
var current_track_index := 0

func _ready() -> void:
	clear_numbers()
	populate_numbers()
	populate_tracks()
	%ActiveSegmentVisualizer.position.y = 0
	
func populate_tracks():
	for t in tracks:
		t.track_switch.disconnect()
		t.current_track_index_changed.disconnect()
		
	tracks.clear()
	for child in %TrackContainer.get_children():
		if child is Track:
			tracks.append(child)
			child.track_switch.connect(go_to_next_track)
			child.current_track_index_changed.connect(_on_current_track_index_changed)
	current_track = 0
	if (tracks.size() > 0):
		tracks[current_track].grab_focus()
	
func go_to_next_track(step:int):
	print("next track!")
	#clear out current visualizations
	tracks[current_track].clear_focus_visualization()
	current_track = (current_track+step) % tracks.size()
	if (current_track < 0):
		current_track = tracks.size()-1
	if (current_track < 0):
		current_track = 0 #in case there is just 1 track
	#and set them for the new track
	tracks[current_track].current_track_index = current_track_index
	tracks[current_track].clear_focus_visualization()
	tracks[current_track].visualize_focused_track_entry()
	tracks[current_track].grab_focus()

func populate_numbers():
	for i in range(64):
		var label = num_scene.instantiate()
		label.set_label(str(i))
		%NumberContainer.add_child(label)

func clear_numbers():
	for child in %NumberContainer.get_children():
		child.queue_free()

func _on_current_track_index_changed(index):
	current_track_index = index
	%ActiveSegmentVisualizer.position.y = current_track_index * 20.0
