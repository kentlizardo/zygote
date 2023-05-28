using Godot;
using System;
using Godot.Collections;
using zygote.game;
using Array = Godot.Collections.Array;

public partial class Root : Control
{
	public static readonly string[] Seeds = {"life"};
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
	[Export] public PackedScene SeedTemplate = null;
	[Export] public HBoxContainer SeedContainer = null;

	public CellNode RootCell
	{
		get => _rootCell;
		set
		{
			_rootCell = value;
			_rootCell.TeamOwner = CellNode.Team.Player;
		}
	}

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
					SeedSelect();
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

	public void SeedSelect()
	{
		Array<CellNode> nodes = new();
		GetNodes(RootCell, ref nodes);
		int treeLevel = nodes.Count;
		if (treeLevel == 0)
		{
			CreateSeed("life");
		}
		else
		{
			Array<string> choices = new();
			for(uint i = 0; i < 3; i++)
				choices.Add(Seeds[GD.Randi() % Seeds.Length]);
			foreach(var choice in choices)
				CreateSeed(choice);
		}
	}

	private void CreateSeed(string seedName)
	{
		var seed = SeedTemplate.Instantiate() as Seed;
		SeedContainer?.AddChild(seed);
		seed.LoadSeed(seedName);
	}

	public double ElapsedTime { get; set; } = 0.0f;

	public CellNode GraftedCell
	{
		get => _graftedCell;
		set
		{
			foreach(var child in SeedContainer.GetChildren().Duplicate())
			{
				if(child is Seed seed)
					seed.Destroy();
			}
			_graftedCell = value;
			if (_graftedCell is not null)
			{
				if (RootCell is null)
				{
					this.AddChild(_graftedCell);
					RootCell = _graftedCell;
					_graftedCell = null;
					CurrentMode = GameMode.Battle;
				}
				else
				{
					CurrentMode = GameMode.Fusion;
				}
			}
		}
	}

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

	public bool TreePaused
	{
		get => _treePaused;
		set
		{
			_treePaused = value;
			GD.Print(_treePaused);
			GetTree().Paused = _treePaused;
		}
	}

	public bool AppPaused
	{
		get => _appPaused;
		set
		{
			_appPaused = value;
			TreePaused = AppPaused || GamePaused;
		}
	}

	public bool GamePaused
	{
		get => _gamePaused;
		set
		{
			_gamePaused = value;
			TreePaused = AppPaused || GamePaused;
		}
	}

	private CellNode _hoveredCell;
	private CellNode _graftedCell;
	private GameMode _currentMode;
	private CellNode _rootCell = null;
	private bool _gamePaused;
	private bool _treePaused;
	private bool _appPaused;

	public Root()
	{
		Instance = this;
	}

	public override void _Ready()
	{
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
		switch (CurrentMode)
		{
			case GameMode.Startup:
				if (Input.IsActionJustPressed("ui_accept"))
					CurrentMode = GameMode.SeedSelection;
				break;
			case GameMode.Fusion:
				if (Input.IsActionJustPressed("click"))
				{
					GD.Print("Pressed!");
					if (HoveredCell is not null)
					{
						this.AddChild(_graftedCell);
						_graftedCell.CellParent = HoveredCell;
						HoveredCell = null;
						CurrentMode = GameMode.Battle;
					}
				}
				break;
		}
		if (Input.IsActionJustPressed("ui_cancel"))
		{
			AppPaused = !AppPaused;
		}
		if (Input.IsActionJustPressed("ui_page_down"))
		{
			CurrentMode = GameMode.SeedSelection;
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
		if (TreePaused) return;
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
