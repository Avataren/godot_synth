using Godot;
using System;

public partial class graph_editor : Node2D
{
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("Node2D is ready!");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
