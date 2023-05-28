using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

namespace zygote.game;

public partial class CellNode : CharacterBody2D
{
	public const float Radius = 16.0f;
	public const float MinBetweenSatelliteDistance = 4.0f;
	public const float OrbitIncrement = 8.0f;
	public const float MinOrbitOffset = 16.0f;

	public const uint NeutralBitMask = 1;
	public const uint PlayerBitMask = 1 << 1;
	public const uint EnemyBitMask = 1 << 2;

	public enum Team
	{
		Neutral,
		Player,
		Enemy,
	}

	public Godot.Collections.Dictionary<Team, uint> TeamMasks = new Godot.Collections.Dictionary<Team, uint>()
	{
		{Team.Neutral, NeutralBitMask},
		{Team.Player, PlayerBitMask},
		{Team.Enemy, EnemyBitMask},
	};


	public enum ConnectionType {
		Orbit,
		Fixed,
		SpiralOrbit,
	}
	
	[Signal]
	public delegate void CellHitEventHandler();
	
	[Export] public string CellType { get; set; }
	[Export] public Sprite2D CellSprite { get; set; }
	[Export] public Line2D LinkLine { get; set; }

	[ExportCategory("Cell Properties")]
	[Export] public float RigidityFactor { get; set; } = 1.0f;
	[Export] public ConnectionType LeafConnectionType { get; set; } = ConnectionType.Fixed;
	[Export] public int MaximumSatellites { get; set; } = 1;
	[Export(PropertyHint.MultilineText)] public string SeedDescription { get; set; } = String.Empty;

	public CellNode CellParent
	{
		get => _cellParent;
		set
		{
			if (_cellParent != null)
			{
				_cellParent.CellChildren.Remove(this);
				_cellParent.UpdateAngle();
				TeamOwner = Team.Neutral;
			}
			LinkLine.Visible = false;
			LinkLine.Modulate = LinkLine.Modulate with { A = 0.0f };
			_cellParent = value;
			if (_cellParent != null)
			{
				_cellParent.CellChildren.Add(this);
				_cellParent.UpdateAngle();
				LinkLine.Visible = true;
				LinkLine.Modulate = LinkLine.Modulate with { A = 0.3f };
				TeamOwner = _cellParent.TeamOwner;
			}
			UpdateAngle();
			UpdateHeight();
		}
	}
	public Team TeamOwner
	{
		get => _teamOwner;
		set {
			_teamOwner = value;
			this.CollisionLayer = TeamMasks[_teamOwner];
			this.CollisionMask = 0;
			foreach (KeyValuePair<Team, uint> pair in TeamMasks)
			{
				if (pair.Key != _teamOwner)
				{
					this.CollisionMask = CollisionMask ^ pair.Value;
				}
			}
			foreach (var cellChild in CellChildren)
			{
				cellChild.TeamOwner = _teamOwner;
			}
		}
	}

	public Array<CellNode> CellChildren { get; set; } = new();
	public int TreeHeight { get; set; } = 0;

	private void UpdateHeight()
	{
		var query = CellParent;
		var height = 0;
		while (query != null)
		{
			query = query.CellParent;
			height++;
		}
		TreeHeight = height;
		this.ProcessPriority = TreeHeight;
	}

	public void UpdateAngle()
	{
		if (CellParent != null)
		{
			var index = CellParent.CellChildren.IndexOf(this);
			var siblingCount = CellParent.CellChildren.Count;
			var fan = CellParent.AngleRange;
			if (CellParent.LeafConnectionType == ConnectionType.Orbit)
				fan = 360.0f;
			var parentAngleIncrement = fan / siblingCount;
			if (siblingCount > 1)
				AngleRangeOffset = parentAngleIncrement * index - 0.5f * fan;
			else
				AngleRangeOffset = parentAngleIncrement * index;
			AngleRange = parentAngleIncrement;
			
			// Collision check for 2 siblings, and iteratively increase orbit distance until there is sufficient distance between 2 neighboring siblings
			OrbitDistance = Radius * 2.0f + MinOrbitOffset;
			if (siblingCount > 1)
			{
				var originA = Vector2.Right * OrbitDistance;
				var originB = Vector2.Right.Rotated(Mathf.DegToRad(parentAngleIncrement)) * OrbitDistance;
				var threshold =
					Radius * 2.0f + MinBetweenSatelliteDistance; // Collision threshold for 2 circles + margin
				while ((originA - originB).LengthSquared() <= threshold * threshold)
				{
					OrbitDistance += OrbitIncrement;

					originA = Vector2.Right * OrbitDistance;
					originB = Vector2.Right.Rotated(Mathf.DegToRad(parentAngleIncrement)) * OrbitDistance;
				}
			}

		}
		else
		{
			AngleRange = 360.0f;
			AngleRangeOffset = 0.0f;
			OrbitDistance = Radius * 2.0f + MinOrbitOffset;
		}
		GD.Print("Updating children for " + this.Name);
		foreach (CellNode cellChild in CellChildren)
		{
			GD.Print("Updating child " + cellChild.Name);
			cellChild.UpdateAngle();
		}
	}
	
	public Vector2 Acceleration { get; set; } = Vector2.Zero;
	public Vector2 Velocity { get; set; } = Vector2.Zero;

	private CellNode _cellParent;
	private Team _teamOwner = Team.Neutral;
	public float AngleRange { get; private set; } = 360.0f;
	public float AngleRangeOffset { get; private set; } = 0.0f;
	public float OrbitDistance { get; set; } = Radius * 2.0f + MinOrbitOffset;

	public override void _Ready()
	{
		CellSprite.Texture = ResourceLoader.Load<Texture2D>("res://assets/textures/" + CellType.ToLower() + ".png");
		if (GetNode("HoverArea") is Area2D hoverArea)
		{
			hoverArea.MouseEntered += MouseEnter;
			hoverArea.MouseExited += MouseExit;
		}
		
		this.Modulate = this.Modulate with { A = 0.0f };
		var tw = CreateTween();
		tw.TweenProperty(this, "modulate:a", 1.0f, 0.25f);
	}

	private void MouseEnter()
	{
		if(Root.Instance.CurrentMode == Root.GameMode.Fusion)
			Root.Instance.HoveredCell = this;
	}
	
	private void MouseExit()
	{
		if(Root.Instance.HoveredCell == this)
			Root.Instance.HoveredCell = null;
	}
	
	public override void _Process(double delta)
	{
		if (CellParent != null)
		{
			LinkLine.GlobalRotation = 0.0f;
			LinkLine.SetPointPosition(0, Vector2.Zero);
			LinkLine.SetPointPosition(1, CellParent.GlobalPosition - GlobalPosition);
		}
	}

	public void Destroy()
	{
		foreach(var child in CellChildren.Duplicate())
		{
			child.CellParent = null;
		}
		if (Root.Instance.RootCell == this)
		{
			Root.Instance.RootCell = null;
		}
		if (CellParent != null)
		{
			CellParent.CellChildren.Remove(this);
			CellParent.UpdateAngle();
		}
		this.CallDeferred("free");
	}
	
	// referencing https://forum.unity.com/threads/clamping-angle-between-two-values.853771/ by orionsyndrome
	private static float ModularClamp(float val, float min, float max, float rangemin = -180f, float rangemax = 180f)
	{
		var modulus = Mathf.Abs(rangemax - rangemin);
		if ((val %= modulus) < 0f)
		{
			val += modulus;
		}

		return Mathf.Clamp(val + Mathf.Min(rangemin, rangemax), min, max);
	}
	
	public override void _PhysicsProcess(double delta)
	{
		if (CellParent != null)
		{
			var parentPos = CellParent.GlobalPosition;
			var targetPosition = GlobalPosition;

			this.Rotation = Vector2.Right.AngleTo(GlobalPosition - parentPos);
			var sourceAngleDegrees = Mathf.Wrap(AngleRangeOffset + CellParent.RotationDegrees, -180f, 180f);
			
			if (CellParent.LeafConnectionType == ConnectionType.Orbit)
			{
				// var dist = parentPos - this.GlobalPosition;
				// targetPosition = parentPos - norm * OrbitDistance;
				var dist = parentPos - this.GlobalPosition;
				var distLength = (dist.Length() + OrbitDistance) * 0.5f;
				var norm = Mathf.RadToDeg(dist.Normalized().Angle());

				var halfAngle = AngleRange * 0.5f;
				var A = sourceAngleDegrees - halfAngle;
				var B = sourceAngleDegrees + halfAngle;
				var angle = ModularClamp(norm, A, B);
				var rads = Mathf.DegToRad(angle);
				
				var orbit = Vector2.FromAngle(rads);
				orbit = (orbit + -dist.Normalized()) * 0.5f;

				targetPosition = parentPos + orbit * distLength;
			} 
			else if (CellParent.LeafConnectionType == ConnectionType.Fixed)
			{
				this.Rotation = Vector2.Right.AngleTo(GlobalPosition - parentPos);
				
				var dist = parentPos - this.GlobalPosition;

				var orbit = Vector2.FromAngle(Mathf.DegToRad(sourceAngleDegrees));
				
				targetPosition = parentPos + orbit * OrbitDistance;
			} 
			else if (CellParent.LeafConnectionType == ConnectionType.SpiralOrbit)
			{
				this.Rotation = Vector2.Right.AngleTo(GlobalPosition - parentPos);
				
				var dist = parentPos - this.GlobalPosition;

				var orbit = Vector2.FromAngle((float)Mathf.DegToRad(sourceAngleDegrees + 360.0 * Mathf.Sin(Root.Instance.ElapsedTime)));
				
				targetPosition = parentPos + orbit * OrbitDistance;
			}
			
			// GlobalPosition = GlobalPosition.Lerp(targetPosition, 0.2f);
			targetPosition = GlobalPosition.Lerp(targetPosition, 0.5f);
			var targetDist = (targetPosition - this.GlobalPosition);
			this.Velocity = targetDist.Normalized() * 10.0f * targetDist.Length() * RigidityFactor;
		}
		this.Velocity += Acceleration * (float)delta;
		this.Position += Velocity * (float)delta;

		MoveAndCollide(Velocity * (float)delta);
	}

}