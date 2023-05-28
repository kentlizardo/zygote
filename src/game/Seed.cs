using Godot;
using System;
using zygote.game;

public partial class Seed : Control
{
	private const float IconHoverRotation = 95.0f;
	private const float IconRotation = 15.0f;
	
	[Export] public RichTextLabel Text { get; set; }
	[Export] public TextureRect Icon { get; set; }

	private float _iconRotation = 0.0f;
	public float IconRotationSpeed { get; private set; } = 15.0f;
	private string SeedName { get; set; }
	private CellNode SeedCell { get; set; }
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		this.Modulate = this.Modulate with { A = 0.0f };
		
		Text.MetaClicked += Click;
		Text.MetaHoverEnded += Unhover;
		Text.MetaHoverStarted += Hover;
	}

	public async void Destroy()
	{
		Text.MetaClicked -= Click;
		Text.MetaHoverEnded -= Unhover;
		Text.MetaHoverStarted -= Hover;
		var tw = CreateTween();
		tw.TweenProperty(this, "modulate:a", 0.0f, 1.0);
		await ToSignal(tw, "finished");
		this.QueueFree();
	}

	public void LoadSeed(string seedName)
	{
		SeedName = seedName;
		Icon.Texture = ResourceLoader.Load<Texture2D>("res://assets/textures/" + seedName.ToLower() + ".png");
		var cellTemplate = ResourceLoader.Load<PackedScene>("res://assets/scenes/cells/" + seedName.ToLower() + ".tscn");
		SeedCell = cellTemplate.Instantiate() as CellNode;
		
		var tween = this.CreateTween();	
		tween.TweenProperty(this, "modulate:a", 1.0f, 1.0f).SetEase(Tween.EaseType.In).SetTrans(Tween.TransitionType.Sine);
	}

	private void Hover(Variant meta)
	{
		var tw = CreateTween();
		tw.Parallel().TweenProperty(this, "IconRotationSpeed", IconHoverRotation, 0.5f);
		tw.Parallel().TweenProperty(Icon, "scale", Vector2.One * 1.2f, 0.5f);
	}
	private void Unhover(Variant meta)
	{
		var tw = CreateTween();
		tw.Parallel().TweenProperty(this, "IconRotationSpeed", IconRotation, 0.5f);
		tw.Parallel().TweenProperty(Icon, "scale", Vector2.One, 0.5f);
	}

	private void Click(Variant meta)
	{
		Root.Instance.GraftedCell = SeedCell;
	}
	
	public override void _Process(double delta)
	{
		_iconRotation += (float)delta * IconRotationSpeed;
		Icon.RotationDegrees = _iconRotation;
	}
}
