extends HBoxContainer


# Called when the node enters the scene tree for the first time.
func _ready() -> void:
	set_cpu_usage(0)

func set_cpu_usage(usage):
	%ColorRect.material.set("shader_parameter/cpu_usage", usage)
	%PercentLabel.text = str(round(usage * 100)) + "%"
