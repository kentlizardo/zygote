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

	public CellNode CellParent
	{
		get => _cellParent;
		set
		{
			if (_cellParent != null)
			{
				_cellParent.CellChildren.Remove(this);
				_cellParent.UpdateAngle();
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
			}
			UpdateAngle();
			UpdateHeight();
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
	private void UpdateAngle()
	{
		if (CellParent != null)
		{
			var index = CellParent.CellChildren.IndexOf(this);
			var siblingCount = CellParent.CellChildren.Count;
			var fan = 360.0f;
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
	public ConnectionType LeafConnectionType { get; private set; } = ConnectionType.Fixed;
	
	private CellNode _cellParent;
	public float AngleRange { get; private set; } = 360.0f;
	public float AngleRangeOffset { get; private set; } = 0.0f;
	public const float MinOrbitOffset = 16.0f;
	public float OrbitDistance { get; set; } = Radius * 2.0f + MinOrbitOffset;

	public override void _Ready()
	{
		CellSprite.Texture = ResourceLoader.Load<Texture2D>("res://assets/textures/" + CellType.ToLower() + ".png");
	}
	
	public override void _Process(double delta)
	{
		
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
	
	public override void _PhysicsProcess(double delta)
	{
		if (CellParent != null)
		{
			var parentPos = CellParent.GlobalPosition;
			this.Rotation = Vector2.Right.AngleTo(GlobalPosition - parentPos);
			LinkLine.GlobalRotation = 0.0f;
			LinkLine.SetPointPosition(0, Vector2.Zero);
			LinkLine.SetPointPosition(1, parentPos - GlobalPosition);

			var targetPosition = GlobalPosition;
			
			if (CellParent.LeafConnectionType == ConnectionType.Orbit)
			{
				var dist = parentPos - this.GlobalPosition;
				var norm = dist.Normalized();
				targetPosition = parentPos - norm * OrbitDistance;
			} 
			else if (CellParent.LeafConnectionType == ConnectionType.Fixed)
			{
				var dist = parentPos - this.GlobalPosition;

				var orbit = Vector2.FromAngle(Mathf.DegToRad(AngleRangeOffset + CellParent.RotationDegrees));
				
				targetPosition = parentPos + orbit * OrbitDistance;
			} 
			else if (CellParent.LeafConnectionType == ConnectionType.SpiralOrbit)
			{
				
			}
			
			var targetDist = (targetPosition - this.GlobalPosition);
			GlobalPosition = GlobalPosition.Lerp(targetPosition, 0.2f);
		}
		this.Velocity += Acceleration * (float)delta;
		this.Position += Velocity * (float)delta;

		MoveAndCollide(Velocity * (float)delta);
	}

}