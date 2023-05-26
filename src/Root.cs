using Godot;
using System;
using zygote.game;

public partial class Root : Node
{
	[Export] public PackedScene RootCellTemplate = null;
	[Export] public Camera2D MainCamera { get; set; }

	private CellNode _rootCell = null;
	
	public override void _Ready()
	{
		_rootCell = RootCellTemplate.Instantiate() as CellNode;
		this.AddChild(_rootCell);
		var subNode = RootCellTemplate.Instantiate() as CellNode;
		_rootCell?.Leaves.AddChild(subNode);
		if (subNode != null) subNode.Position = Vector2.One * 5.0f;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (_rootCell != null)
			if (MainCamera is not null)
				MainCamera.Position = MainCamera.Position.Lerp(_rootCell.Position, 0.5f);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (_rootCell != null)
			_rootCell.DriveAcceleration += Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").Normalized() * 5.0f;
	}
}
