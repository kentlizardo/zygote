using Godot;
using System;

public partial class Seed : Control
{
	public string SeedName { get; set; }

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		var tween = this.CreateTween();
		this.Modulate = this.Modulate with { A = 0.0f };
		tween.TweenProperty(this, "modulate:a", 1.0f, 1.0f);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
