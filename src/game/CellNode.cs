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
	public const float MoveSpeed = 100.0f;
	
	public const float Radius = 16.0f;
	public const float MinBetweenSatelliteDistance = 4.0f;
	public const float OrbitIncrement = 8.0f;
	public const float MinOrbitOffset = 32.0f;

	public const uint NeutralBitMask = 1;
	public const uint PlayerBitMask = 1 << 1;
	public const uint EnemyBitMask = 1 << 2;

	public const float DamageIFrames = 1.5f;

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
	[Export] public Node2D CellShieldSprites { get; set; }
	[Export] public Node2D CellDamageSprites { get; set; }
	[Export] public Line2D LinkLine { get; set; }

	[ExportCategory("Cell Properties")]
	[Export] public float RigidityFactor { get; set; } = 1.0f;
	[Export] public ConnectionType LeafConnectionType { get; set; } = ConnectionType.Fixed;
	[Export] public int MaximumSatellites { get; set; } = 1;

	[Export]
	public int BaseDamage
	{
		get => _baseDamage;
		set
		{
			_baseDamage = value;
			RefreshDamage();
		}
	}

	public int DamageBonus
	{
		get => _damageBonus;
		set
		{
			_damageBonus = value;
			RefreshDamage();
		}
	}

	private void RefreshDamage()
	{
		Damage = BaseDamage + DamageBonus;
	}

	public int Damage
	{
		get => _damage;
		set
		{
			_damage = value;
			if (CellDamageSprites is Node2D sprites)
			{
				for (int index = 0; index < sprites.GetChildren().Count; index++)
				{
					if (Damage > index)
					{
						var child = sprites.GetChildren()[index] as Sprite2D;
						if (child != null) child.Modulate = child.Modulate with { A = 0.7f };
					}
					else
					{
						var child = sprites.GetChildren()[index] as Sprite2D;
						if (child != null) child.Modulate = child.Modulate with { A = 0.0f };
					}
				}
			}
			// if (Damage > 0)
			// 	CellShieldSprite.Modulate = CellShieldSprite.Modulate with
			// 	{
			// 		R = 1.0f,
			// 		G = 0.5f,
			// 		B = 0.5f,
			// 	};
			// else
			// 	CellShieldSprite.Modulate = CellShieldSprite.Modulate with
			// 	{
			// 		R = 1.0f,
			// 		G = 1.0f,
			// 		B = 1.0f,
			// 	};
		}
	}

	[Export]
	public int Shield
	{
		get => _shield;
		set
		{
			_shield = value;
		}
	}

	public int Life
	{
		get => _life;
		set
		{
			_life = value;
			if (Life <= 0)
			{
				this.CallDeferred("Destroy");
			}
			else
			{
				if (CellShieldSprites is Node2D sprites)
				{
					for (int index = 0; index < sprites.GetChildren().Count; index++)
					{
						if (Life > index)
						{
							var child = sprites.GetChildren()[index] as Sprite2D;
							if (child != null) child.Modulate = child.Modulate with { A = 1.0f };
						}
						else
						{
							var child = sprites.GetChildren()[index] as Sprite2D;
							if (child != null) child.Modulate = child.Modulate with { A = 0.0f };
						}
					}
				}
			}
		}
	}

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
			if (TeamOwner == Team.Neutral)
				Drive = Vector2.Zero;
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

	public float InvincibleFrames
	{
		get => _invincibleFrames;
		set
		{
			_invincibleFrames = value;
			CellShieldSprites.Modulate = CellShieldSprites.Modulate with { A = (DamageIFrames - InvincibleFrames) / DamageIFrames };
		}
	}

	public float CooldownFrames
	{
		get => _cooldownFrames;
		set
		{
			_cooldownFrames = value;
			CellDamageSprites.Modulate = CellDamageSprites.Modulate with { A = (DamageIFrames - CooldownFrames) / DamageIFrames };
		}
	}

	public void Regenerate()
	{
		this.Life = this.Shield;
		if (CellChildren.Count < MaximumSatellites)
		{
			if (this.CellType == "vine")
			{
				var cellTemplate = ResourceLoader.Load<PackedScene>("res://assets/scenes/cells/vine.tscn");
				if (cellTemplate.Instantiate() is CellNode newCell)
				{
					newCell.GlobalPosition = GlobalPosition +
					                         Vector2.FromAngle(GD.Randf() * Mathf.Tau) * GD.Randf() *
					                         128.0f;
					Root.Instance.AddChild(newCell);
					newCell.CellParent = this;
				}
			}
			
			if (this.CellType == "virus")
			{
				var cellTemplate = ResourceLoader.Load<PackedScene>("res://assets/scenes/cells/virus.tscn");
				if (cellTemplate.Instantiate() is CellNode newCell)
				{
					newCell.GlobalPosition = GlobalPosition +
					                         Vector2.FromAngle(GD.Randf() * Mathf.Tau) * GD.Randf() *
					                         64.0f;
					Root.Instance.AddChild(newCell);
					newCell.CellParent = this;
				}

				if (CellChildren.Count < MaximumSatellites)
				{
					if (cellTemplate.Instantiate() is CellNode newCell2)
					{
						newCell2.GlobalPosition = GlobalPosition +
						                          Vector2.FromAngle(GD.Randf() * Mathf.Tau) * GD.Randf() *
						                          64.0f;
						Root.Instance.AddChild(newCell2);
						newCell2.CellParent = this;
					}
				}
				
			}
			
			if (this.CellType == "ice")
			{
				var cellTemplate = ResourceLoader.Load<PackedScene>("res://assets/scenes/cells/comet.tscn");
				if (cellTemplate.Instantiate() is CellNode newCell)
				{
					newCell.GlobalPosition = GlobalPosition +
					                         Vector2.FromAngle(GD.Randf() * Mathf.Tau) * GD.Randf() *
					                         128.0f;
					Root.Instance.AddChild(newCell);
					newCell.CellParent = this;
				}
			}

			if (this.CellType == "rock")
			{
				var cellTemplate = ResourceLoader.Load<PackedScene>("res://assets/scenes/cells/asteroid.tscn");
				if (cellTemplate.Instantiate() is CellNode newCell)
				{
					newCell.GlobalPosition = GlobalPosition +
					                         Vector2.FromAngle(GD.Randf() * Mathf.Tau) * GD.Randf() *
					                         128.0f;
					Root.Instance.AddChild(newCell);
					newCell.CellParent = this;
				}
			}
		}
	}
	
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
			if (CellParent.LeafConnectionType == ConnectionType.Orbit || CellParent.LeafConnectionType == ConnectionType.SpiralOrbit)
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
		foreach (CellNode cellChild in CellChildren)
		{
			cellChild.UpdateAngle();
		}
	}

	public Vector2 Drive { get; set; } = Vector2.Zero;
	public Vector2 Acceleration { get; set; } = Vector2.Zero;
	public Vector2 Velocity { get; set; } = Vector2.Zero;

	private CellNode _cellParent;
	private Team _teamOwner = Team.Neutral;
	private float _invincibleFrames = 0.0f;
	private float _cooldownFrames = 0.0f;
	private int _life;
	private int _baseDamage = 0;
	private int _shield = 1;
	private int _damageBonus = 0;
	private int _damage = 0;
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

		Life = Shield;
		RefreshDamage();
	}

	private void MouseEnter()
	{
		if(this.TeamOwner == Team.Player)
			if(Root.Instance.CurrentMode == Root.GameMode.Fusion && this.CellChildren.Count < this.MaximumSatellites)
				Root.Instance.HoveredCell = this;
	}
	
	private void MouseExit()
	{
		if(Root.Instance.HoveredCell == this)
			Root.Instance.HoveredCell = null;
	}
	
	public override void _Process(double delta)
	{
		if (CellParent is not null)
		{
			LinkLine.GlobalRotation = 0.0f;
			LinkLine.SetPointPosition(0, Vector2.Zero);
			LinkLine.SetPointPosition(1, CellParent.GlobalPosition - GlobalPosition);
		}
	}

	public void Destroy()
	{
		if (Root.Instance.RootCell == this)
		{
			Root.Instance.RootCell = null;
		}
		if (Root.Instance.RootEnemy == this)
		{
			Root.Instance.RootEnemy = null;
		}
		if (Root.Instance.HoveredCell == this)
		{
			Root.Instance.HoveredCell = null;
		}
		if (Root.Instance.GraftedCell == this)
		{
			Root.Instance.GraftedCell = null;
		}
		foreach(var child in CellChildren.Duplicate())
		{
			if (CellType == "branch")
				child.CellParent = CellParent;
			else
				child.CellParent = null;
		}
		if (CellParent is not null)
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
		if (Root.Instance.CurrentMode == Root.GameMode.Fusion ||
		    Root.Instance.CurrentMode == Root.GameMode.SeedSelection)
			return;
		if(InvincibleFrames > 0.0f)
			InvincibleFrames -= (float)delta;
		if(CooldownFrames > 0.0f)
			CooldownFrames -= (float)delta;
		if (CellParent is not null)
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

				var orbit = Vector2.FromAngle((float)Mathf.DegToRad(sourceAngleDegrees + 180.0 * Root.Instance.ElapsedTime));
				
				targetPosition = parentPos + orbit * OrbitDistance;
			}
			
			// GlobalPosition = GlobalPosition.Lerp(targetPosition, 0.2f);
			targetPosition = GlobalPosition.Lerp(targetPosition, 0.5f);
			var targetDist = (targetPosition - this.GlobalPosition);
			this.Velocity = targetDist.Normalized() * 10.0f * targetDist.Length() * RigidityFactor;
		}
		else
		{
			this.Acceleration = Drive * MoveSpeed;
		}
		if(Acceleration.Dot(Velocity) <= 0)
			this.Velocity += Acceleration * (float)delta;
		else
		{
			if (Velocity.LengthSquared() <= 10000f)
			{
				this.Velocity += Acceleration * (float)delta;
			}
		}
		if (CellType == "spike")
		{
			if (this.Velocity.Length() >= 400)
			{
				DamageBonus = 3;
			}
			else if (this.Velocity.Length() >= 200)
			{
				DamageBonus = 2;
			}
			else if (this.Velocity.Length() >= 100)
			{
				DamageBonus = 1;
			}
			else
			{
				DamageBonus = 0;
			}
		}
		else
		{
			if (this.Velocity.Length() >= 500)
			{
				DamageBonus = 3;
			}
			else if (this.Velocity.Length() >= 250)
			{
				DamageBonus = 2;
			}
			else if (this.Velocity.Length() >= 150)
			{
				DamageBonus = 1;
			}
			else
			{
				DamageBonus = 0;
			}
			if(CellParent == null && TeamOwner == Team.Player)
				GD.Print(Damage);
		}
		
		this.Position += Velocity * (float)delta;

		var collide = MoveAndCollide(Velocity * (float)delta);
		if (collide is not null)
		{
			if (collide.GetCollider() is CellNode node)
			{
				if (TeamOwner != node.TeamOwner && TeamOwner != Team.Neutral)
				{
					// if (TeamOwner == Team.Player && node.TeamOwner == Team.Neutral)
					// {
					// 	Root.Instance.GraftedCell = node;
					// }
					// else
					// if (InvincibleFrames <= 0.0f && node.Damage > 0 && node.CooldownFrames <= 0.0f)
					// {
					// 	GD.Print(TeamOwner + " colliding with " + node.TeamOwner);
					// 	node.CooldownFrames = DamageIFrames;
					// 	InvincibleFrames = DamageIFrames;
					// 	TakeDamage(node.Damage);
					// }
					if (node.InvincibleFrames <= 0.0f && Damage > 0 && CooldownFrames <= 0.0f)
					{
						// if (node.Shield <= Damage)
						// {
							CooldownFrames = DamageIFrames;
							node.InvincibleFrames = DamageIFrames;
							node.TakeDamage(Damage);
						//}
					}
				}
			}
		}
	}

	public void TakeDamage(int dmg)
	{
		if (this.CellType == "rock")
		{
			var cellTemplate = ResourceLoader.Load<PackedScene>("res://assets/scenes/cells/asteroid.tscn");
			var newCell = cellTemplate.Instantiate() as CellNode;
			if (newCell != null)
			{
				newCell.GlobalPosition = GlobalPosition +
				                         Vector2.FromAngle(GD.Randf() * Mathf.Tau) * GD.Randf() *
				                         64.0f;
				if (newCell.GetParent() is null)
					Root.Instance.AddChild(newCell);
				newCell.CellParent = this;
			}
		}
		this.Life -= dmg;
	}

}