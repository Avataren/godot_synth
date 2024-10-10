using Godot;
using System;
using Synth;

public partial class SynthInterrop : Node
{
	public static AudioParam GetEnumFromString(string enumName)
	{
		// Parse the string to get the corresponding enum value
		return (AudioParam)System.Enum.Parse(typeof(AudioParam), enumName);
	}	
}
