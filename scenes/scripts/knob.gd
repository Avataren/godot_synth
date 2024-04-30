extends Control

var mouse_hovered:= false
var mouse_drag:= false
# Called when the node enters the scene tree for the first time.
#func _ready():
	#pass # Replace with function body.
#
#
## Called every frame. 'delta' is the elapsed time since the previous frame.
#func _process(delta):
	#pass

func _unhandled_input(event):
	if mouse_hovered:
		if (event.is_action_pressed('mouse_left')):
				print("click!")
				_on_start_drag()
	if mouse_drag:				
		if (event.is_action_released('mouse_left')):
				_on_stop_drag()
		
func _on_start_drag():
	mouse_drag = true
	print ("start drag!")
	
func _on_stop_drag():
	mouse_drag = false	
	print ("stop drag!")

func _on_mouse_entered():
	mouse_hovered = true
	print ("mouse entered")


func _on_mouse_exited():
	mouse_hovered = false
	print ("mouse left")

