extends Control

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	populate_numbers()

func populate_numbers():
	pass
	#for i in range(64):
		#var label = Label.new()
		#label.text = str(i)
		#label.add_theme_font_size_override("px", 14)
		#%NumberContainer.add_child(label)
