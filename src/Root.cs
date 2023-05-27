using Godot;
using System;
using System.Collections;
using Godot.Collections;
using zygote.game;
using Array = Godot.Collections.Array;

public partial class Root : Node2D
{
	[Export] public PackedScene RootCellTemplate = null;
	[Export] public Camera2D MainCamera { get; set; }

	public CellNode RootCell { get; set; } = null;

	public static Root Instance { get; private set; }
	public Root()
	{
		Instance = this;
	}
	
	public override void _Ready()
	{
		RootCell = RootCellTemplate.Instantiate() as CellNode;
		this.AddChild(RootCell, true);
		for (int i = 0; i < 40; i++)
		{
			RandomBranch(RootCell);
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

	public void RandomDelete(CellNode tree)
	{
		Array<CellNode> all = new();
		GetNodes(tree, ref all);
		var newParent = all.PickRandom();
		newParent.Destroy();
	}
	
	public void GraftNewNode(CellNode parentNode = null)
	{
		var subNode = RootCellTemplate.Instantiate() as CellNode;
		subNode.CellSprite.Modulate = subNode.CellSprite.Modulate with { A = 0.5f };
		this.AddChild(subNode, true);
		if(parentNode != null)
			subNode.CellParent = parentNode;
	}
	
	public override void _Process(double delta)
	{
		if (RootCell != null)
			if (MainCamera is not null)
				MainCamera.Position = MainCamera.Position.Lerp(RootCell.Position, 0.8f);
		if (Input.IsActionJustPressed("ui_cancel"))
		{
			Graft();
		}
		if (Input.IsActionJustPressed("ui_home"))
		{
			Branch();
		}

		if (Input.IsActionJustPressed("ui_text_backspace"))
		{
			RandomDelete(RootCell);
		}
		if (Input.IsActionJustPressed("ui_accept"))
		{
			Array<CellNode> nodes = new();
			GetLeaves(RootCell, ref nodes);
			foreach(var i in nodes)
			{
				GD.Print("Grafting to leaf " + i.Name);
				GraftNewNode(i);
			}
		}
 	}
	private void GetNodes(CellNode cell, ref Array<CellNode> array)
	{
		array.Add(cell);
		foreach (CellNode child in cell.CellChildren)
		{
			GetNodes(child, ref array);
		}
	}
	private void GetLeaves(CellNode cell, ref Array<CellNode> array)
	{
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

	public void Graft()
	{
		GraftNewNode(RootCell);
	}
	
	public void Branch()
	{
		Array<CellNode> nodes = new();
		GetLeaves(RootCell, ref nodes);
		
		Array<CellNode> secondToLast = new();
		foreach(var i in nodes)
		{
			if (i.CellParent != null)
			{
				if(!secondToLast.Contains(i.CellParent))
					secondToLast.Add(i.CellParent);
			}
		}

		foreach (var i in secondToLast)
		{
			GraftNewNode(i);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (RootCell != null)
		{
			//_rootCell.Acceleration = Input.GetAxis("ui_down", "ui_up") * Vector2.Right.Rotated(_rootCell.Rotation) * 100.0f;
			//_rootCell.Rotate((float)(Input.GetAxis("ui_left", "ui_right") * delta) * 10.0f);
			RootCell.LookAt(GetGlobalMousePosition());
			RootCell.Acceleration = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").Normalized() * 100.0f;
		}
	}
}
