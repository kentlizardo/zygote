using Godot;
using Godot.Collections;
using CellNode = worldtrees.game.CellNode;
using Vector2 = Godot.Vector2;

namespace worldtrees;

public partial class Root : Control
{
	public static readonly string[] Seeds =
	{
		"life",
		"branch",
		"magnet",
		"vine",
		"star",
		"virus",
		"nebulous",
		
		"leaf",
		"spacestation",
		"ice",
		"spike",
		"rock",
		"magma",
	};
	public static readonly string[] StructureSeeds =
	{
		"life",
		"branch",
		"magnet",
		"vine",
		"star",
		"virus",
		"nebulous",
	};
	public static readonly string[] StarterSeeds =
	{
		"leaf",
		"spacestation",
		"ice",
		"nebulous",
		"spike",
		"rock",
		"magma",
	};
	
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
	[Export] public TextureRect SplashRect = null;
	[Export] public Sprite2D WaveFinder = null;
	[Export] public RichTextLabel Subtitle = null;
	[Export] public AudioStreamPlayer2D FuseSound { get; set; }

	public CellNode RootCell
	{
		get => _rootCell;
		set
		{
			_rootCell = value;
			if(_rootCell is not null)
				_rootCell.TeamOwner = CellNode.Team.Player;
			else
				CurrentMode = GameMode.Lose;
		}
	}
	
	public CellNode RootEnemy
	{
		get => _rootEnemy;
		set
		{
			if (_rootEnemy is not null && value is null)
			{
				CurrentMode = GameMode.SeedSelection;
			}
			_rootEnemy = value;
			if (_rootEnemy != null) _rootEnemy.TeamOwner = CellNode.Team.Enemy;
		}
	}

	public bool tutorialDone = false;
	private int highScore = 0;
	public GameMode CurrentMode
	{
		get => _currentMode;
		set
		{
			switch (_currentMode)
			{
				case GameMode.Startup:
					highScore = 0;
					var tw = CreateTween();
					tw.Parallel().TweenProperty(SplashRect, "modulate:a", 0.0f, 1.0f).SetTrans(Tween.TransitionType.Expo);
					tw.Parallel().TweenProperty(Subtitle, "modulate:a", 0.0f, 1.0f).SetTrans(Tween.TransitionType.Expo);
					Subtitle.Modulate = Subtitle.Modulate with { A = 1.0f };
					Subtitle.Text = "";
					break;
				case GameMode.Lose:
					var tw3 = CreateTween();
					tw3.Parallel().TweenProperty(SplashRect, "modulate:a", 0.0f, 1.0f).SetTrans(Tween.TransitionType.Expo);
					tw3.Parallel().TweenProperty(Subtitle, "modulate:a", 0.0f, 1.0f).SetTrans(Tween.TransitionType.Expo);
					Subtitle.Modulate = Subtitle.Modulate with { A = 1.0f };
					Subtitle.Text = "";
					break;
				case GameMode.SeedSelection:
					//GamePaused = false;
					break;
				case GameMode.Fusion:
					var tw2 = CreateTween();
					Subtitle.Modulate = Subtitle.Modulate with { A = 1.0f };
					tw2.Parallel().TweenProperty(Subtitle, "modulate:a", 0.0f, 1.0f).SetTrans(Tween.TransitionType.Expo);
					Subtitle.Text = "";
					//GamePaused = false;
					break;
			}
			_currentMode = value;
			switch (_currentMode)
			{
				case GameMode.Startup:
					var tw = CreateTween();
					SplashRect.Modulate = SplashRect.Modulate with { A = 0.0f };
					tw.Parallel().TweenProperty(SplashRect, "modulate:a", 1.0f, 1.0f).SetTrans(Tween.TransitionType.Expo);
					
					Subtitle.Modulate = Subtitle.Modulate with { A = 0.0f };
					tw.Parallel().TweenProperty(Subtitle, "modulate:a", 1.0f, 1.0f).SetTrans(Tween.TransitionType.Expo);
					Subtitle.Text = "[center]press space->start.[/center]\n\n";
					break;
				case GameMode.SeedSelection:
					SeedSelect();
					//GamePaused = true;
					break;
				case GameMode.Fusion:
					var tw2 = CreateTween();
					
					Subtitle.Modulate = Subtitle.Modulate with { A = 0.0f };
					tw2.Parallel().TweenProperty(Subtitle, "modulate:a", 1.0f, 1.0f).SetTrans(Tween.TransitionType.Expo);
					Subtitle.Text = "[center]use your mouse to select a node to fuse into.[/center]\n\n";
					//GamePaused = true;
					break;
				case GameMode.Battle:
					CreateEnemy();
					Array<CellNode> playerNodes = new();
					GetNodes(RootCell, ref playerNodes);
					foreach (var node in playerNodes)
					{
						node.Regenerate();
					}
					if (playerNodes.Count > highScore)
					{
						highScore = playerNodes.Count;
					}

					if (!tutorialDone)
					{
						var tw4 = CreateTween();
						Subtitle.Modulate = Subtitle.Modulate with { A = 0.0f };
						tw4.Parallel().TweenProperty(Subtitle, "modulate:a", 1.0f, 1.0f).SetTrans(Tween.TransitionType.Expo);
						Subtitle.Text = "[center]when you destroy enemy worldtrees,\nyou gain a planet seed to fuse into your worldtree.[/center]\n\n";
						tutorialDone = true;
					}
					break;
				case GameMode.Lose:
					var tw3 = CreateTween();
					tw3.Parallel().TweenProperty(SplashRect, "modulate:a", 1.0f, 1.0f).SetTrans(Tween.TransitionType.Expo);

					Subtitle.Modulate = Subtitle.Modulate with { A = 0.0f };
					tw3.Parallel().TweenProperty(Subtitle, "modulate:a", 1.0f, 1.0f).SetTrans(Tween.TransitionType.Expo);
					Subtitle.Text = $"[center]the highest number of planets in your world tree was: \n{highScore}\npress space to start again.\n[/center]\n\n";
					highScore = 0;
					break;
			}
		}
	}

	private void CreateEnemy()
	{
		Array<CellNode> playerNodes = new();
		GetNodes(RootCell, ref playerNodes);
		int playerLevel = playerNodes.Count;

		var randomLeaf = StructureSeeds[GD.Randi() % StructureSeeds.Length];
		var cellTemplate = ResourceLoader.Load<PackedScene>("res://assets/scenes/cells/" + randomLeaf + ".tscn");
		RootEnemy = cellTemplate.Instantiate() as CellNode;
		
		var pos = RootCell.GlobalPosition + Vector2.FromAngle(Mathf.Tau * GD.Randf()) * (GD.Randf() * 1024 + 1024);
		if (RootEnemy != null)
		{
			RootEnemy.GlobalPosition = pos;
			this.AddChild(RootEnemy);
		}

		var proportion = GD.Randf();
		int minimumStructures = (int)(proportion * (playerLevel - 1));
		int randomNodes = (playerLevel - 1) - minimumStructures;

		for (int i = 0; i < minimumStructures; i++)
		{
			GraftEnemy(true);
		}
		for (int i = 0; i < randomNodes; i++)
		{
			GraftEnemy(false);
		}

		Array<CellNode> enemyNodes = new();
		GetNodes(RootEnemy, ref enemyNodes);
		foreach (var node in enemyNodes)
		{
			node.Regenerate();
		}
	}

	private void GraftEnemy(bool structureOnly)
	{
		Array<CellNode> nodes = new Array<CellNode>();
		GetNodes(RootEnemy, ref nodes);
		
		var newParent = nodes.PickRandom();
		
		string randomLeaf = Seeds[GD.Randi() % Seeds.Length];
		if(structureOnly)
			randomLeaf = StructureSeeds[GD.Randi() % StructureSeeds.Length];
		var cellTemplate = ResourceLoader.Load<PackedScene>("res://assets/scenes/cells/" + randomLeaf + ".tscn");
		var newCell = cellTemplate.Instantiate() as CellNode;
		if (newParent != RootEnemy)
		{
			newCell.GlobalPosition = newParent.GlobalPosition + Vector2.FromAngle(GD.Randf() * Mathf.Tau) * 128.0f;
		}
		else
		{
			newCell.GlobalPosition = newParent.GlobalPosition + (newParent.GlobalPosition - RootEnemy.GlobalPosition).Normalized() * 128.0f;
		}
		this.AddChild(newCell);
		newCell.CellParent = newParent;
	}

	private bool createdLife = false;
	
	public async void SeedSelect()
	{
		if (SeedContainer.GetChildren().Count > 0)
		{
			while (SeedContainer.GetChildren().Count != 0)
				await ToSignal(GetTree(), "process_frame");
		}
		
		Array<CellNode> nodes = new();
		GetNodes(RootCell, ref nodes);
		int treeLevel = nodes.Count;
		if (treeLevel == 0)
		{
			if (!createdLife)
			{
				CreateSeed("life");
				createdLife = true;
				if (!tutorialDone)
				{
					var tw2 = CreateTween();
					Subtitle.Modulate = Subtitle.Modulate with { A = 0.0f };
					tw2.Parallel().TweenProperty(Subtitle, "modulate:a", 1.0f, 1.0f).SetTrans(Tween.TransitionType.Expo);
					Subtitle.Text = "[center]this is your root planet.\nif it becomes destroyed, you lose.\nuse the arrow keys to move your world tree.[/center]\n\n";
				}
			}
			else
			{
				CreateSeed(StructureSeeds[GD.Randi() % StructureSeeds.Length]);
			}
		}
		else if (treeLevel == 1)
		{
			Array<string> choices = new();
			while (choices.Count < 3)
			{
				var seed = StarterSeeds[GD.Randi() % StarterSeeds.Length];
				if(!choices.Contains(seed))
					choices.Add(seed);
			}
			foreach(var choice in choices)
				CreateSeed(choice);
			if (!tutorialDone)
			{
				var tw2 = CreateTween();
				Subtitle.Modulate = Subtitle.Modulate with { A = 0.0f };
				tw2.Parallel().TweenProperty(Subtitle, "modulate:a", 1.0f, 1.0f).SetTrans(Tween.TransitionType.Expo);
				Subtitle.Text =
					"[center]these planets will join your world tree on a mission to conquer the universe!\nuse your mouse to maneuver your world tree and swing them at your enemies.[/center]\n\n";
			}
		}
		else
		{
			Array<string> choices = new();
			while (choices.Count < 3)
			{
				var seed = Seeds[GD.Randi() % Seeds.Length];
				if (GD.Randf() <= 0.75f)
					seed = StructureSeeds[GD.Randi() % StructureSeeds.Length];
				if(!choices.Contains(seed))
					choices.Add(seed);
			}
			foreach(var choice in choices)
				CreateSeed(choice);
		}
	}

	private void CreateSeed(string seedName)
	{
		var seed = SeedTemplate.Instantiate() as game.Seed;
		SeedContainer?.AddChild(seed);
		if (seed != null) seed.LoadSeed(seedName);
	}

	public double ElapsedTime { get; set; } = 0.0f;

	public CellNode GraftedCell
	{
		get => _graftedCell;
		set
		{
			foreach(var child in SeedContainer.GetChildren().Duplicate())
			{
				if(child is game.Seed seed)
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
					
					CurrentMode = GameMode.SeedSelection;
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
	private CellNode _rootEnemy = null;
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
			case GameMode.Lose:
				if (Input.IsActionJustPressed("ui_accept"))
					CurrentMode = GameMode.SeedSelection;
				break;
			case GameMode.Fusion:
				if (Input.IsActionJustPressed("click"))
				{
					GD.Print("Pressed!");
					if (HoveredCell is not null)
					{
						if (RootCell is not null)
							_graftedCell.GlobalPosition = RootCell.GlobalPosition +
							                              Vector2.FromAngle(GD.Randf() * Mathf.Tau) * GD.Randf() *
							                              256.0f;
						if (_graftedCell.GetParent() is null)
							this.AddChild(_graftedCell);
						_graftedCell.CellParent = HoveredCell;
						FuseSound.Play();
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
			RootCell.Drive = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down").Normalized();

			if (RootEnemy is null) return;
			var hostile = RootCell.GlobalPosition - RootEnemy.GlobalPosition;
			RootEnemy.Rotation = (float)Mathf.LerpAngle(RootEnemy.Rotation, hostile.Angle(), 0.1f * delta * 25.0f);
			RootEnemy.Drive = hostile.Normalized();

			var enemyDist = RootEnemy.GlobalPosition - RootCell.GlobalPosition;
			WaveFinder.Rotation = (enemyDist).Angle();
			WaveFinder.Visible = true;
			const float rec = 1f / 256f;
			WaveFinder.Modulate = WaveFinder.Modulate with { A = rec * (enemyDist.Length() - 256.0f) };
		}
		else
		{
			WaveFinder.Visible = false;
		}
	}
}