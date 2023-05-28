using Godot;
using System;
using System.Collections;
using Godot.Collections;
using zygote.game;
using Array = Godot.Collections.Array;

public partial class Root : Control
{
	public enum GameMode
	{
		Startup,
		SeedSelection,
		Fusion,
		Battle,
		Lose,
	}
	public static Root Instance { get; private set; }

	[Export] public PackedScene RootCellTemplate = null;
	[Export] public Camera2D MainCamera { get; set; }

	public CellNode RootCell { get; set; } = null;

	public GameMode CurrentMode
	{
		get => _currentMode;
		set
		{
			_currentMode = value;
			switch (_currentMode)
			{
				case GameMode.Startup:
					break;
				case GameMode.SeedSelection:
					break;
				case GameMode.Fusion:
					break;
				case GameMode.Battle:
					break;
				case GameMode.Lose:
					break;
			}
		}
	}

	public double ElapsedTime { get; set; } = 0.0f;

	public CellNode HoveredCell
	{
		get => _hoveredCell;
		set
		{
			if (_hoveredCell is not null)
			{
				var tween = _hoveredCell.CreateTween();
				tween.TweenProperty(_hoveredCell, "modulate:v", 1.0f, 0.1f);
				_hoveredCell.Modulate = _hoveredCell.Modulate with { V = 1.0f };
				tween.TweenProperty(_hoveredCell, "modulate:a", 1.0f, 0.1f);
			}
			_hoveredCell = value;
			if (_hoveredCell is not null)
			{
				var tween = _hoveredCell.CreateTween();
				tween.TweenProperty(_hoveredCell, "modulate:v", 5.0f, 0.1f);
				tween.TweenProperty(_hoveredCell, "modulate:a", 0.5f, 0.1f);
			}
		} 
	}
	
	private CellNode _hoveredCell;
	private GameMode _currentMode;

	public Root()
	{
		Instance = this;
	}
	
	public override void _Ready()
	{
		RootCell = RootCellTemplate.Instantiate() as CellNode;
		this.AddChild(RootCell, true);

		CurrentMode = GameMode.Startup;
		for (int i = 0; i < 40; i++)
		{
			//RandomBranch(RootCell);
		}
	}

	public void RandomBranch(CellNode tree)
	{
		Array<CellNode> all = new();
		GetNodes(tree, ref all);
		var newParent = all.PickRandom();
		while (newParent.TreeHeight >= 3)
		{
			newParent = all.PickRandom();
		}
		newParent.RotationDegrees = GD.Randf() * 360.0f;
		GraftNewNode(newParent);
		
	}
	
	public void GraftNewNode(CellNode parentNode = null)
	{
		var subNode = RootCellTemplate.Instantiate() as CellNode;
		subNode.CellSprite.Modulate = subNode.CellSprite.Modulate with { A = 1.0f };
		this.AddChild(subNode, true);
		if(parentNode != null)
			subNode.CellParent = parentNode;
	}
	
	public override void _Process(double delta)
	{
		ElapsedTime += delta;
		if (RootCell != null)
			if (MainCamera is not null)
				MainCamera.Position = MainCamera.Position.Lerp(RootCell.Position, 0.8f);
		if (Input.IsActionJustPressed("ui_cancel"))
		{
			GetTree().Paused = !GetTree().Paused;
			GD.Print(GetTree().Paused);
		}
	}
	private void GetNodes(CellNode cell, ref Array<CellNode> array)
	{
		if (cell is null)
			return;
		array.Add(cell);
		foreach (CellNode child in cell.CellChildren)
		{
			GetNodes(child, ref array);
		}
	}
	private void GetLeaves(CellNode cell, ref Array<CellNode> array)
	{
		if (cell is null)
			return;
		if (cell.CellChildren.Count > 0)
		{
			foreach (CellNode child in cell.CellChildren)
			{
				GetLeaves(child, ref array);
			}
		}
		else
		{
			array.Add(cell);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (CurrentMode == GameMode.Battle)
		{
			//_rootCell.Acceleration = Input.GetAxis("ui_down", "ui_up") * Vector2.Right.Rotated(_rootCell.Rotation) * 100.0f;
			//_rootCell.Rotate((float)(Input.GetAxis("ui_left", "ui_right") * delta) * 10.0f);
			var look = GetGlobalMousePosition() - RootCell.GlobalPosition;
			RootCell.Rotation = (float)Mathf.LerpAngle(RootCell.Rotation, look.Angle(), 0.1f * delta * 25.0f);
			RootCell.Acceleration = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").Normalized() * 100.0f;
		}
	}
}
