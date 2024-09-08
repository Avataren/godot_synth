extends Control

@onready var num_scene = preload("res://scenes/tracker/num_entry.tscn")
# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	clear_numbers()
	populate_numbers()

func populate_numbers():
	for i in range(64):
		var label = num_scene.instantiate()
		label.set_label(str(i))
		%NumberContainer.add_child(label)

func clear_numbers():
	for child in %NumberContainer.get_children():
		child.queue_free()
