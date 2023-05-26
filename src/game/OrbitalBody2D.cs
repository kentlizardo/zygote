using Godot;
using System;
using System.ComponentModel;

public partial class OrbitalBody2D : CharacterBody2D
{
	[Export] public Leaves Leaves { get; set; }

	private float _terminalVelocitySquared = Mathf.Pow(20.0f, 2);
	public float Mass { get; set; } = 1.0f;
	public Vector2 Acceleration { get; set; } = Vector2.Zero;
	public Vector2 DriveAcceleration { get; set; } = Vector2.Zero;
	
	// Get the gravity from the project settings to be synced with RigidBody nodes.
	public float gravity = ProjectSettings.GetSetting("physics/2d/default_gravity").AsSingle();
	private OrbitalBody2D _orbitalParent;

	public OrbitalBody2D OrbitalParent
	{
		get => _orbitalParent;
		private set
		{
			_orbitalParent = value;
			GD.Print(this.Name + ": orbital is now " + value.Name);
		}
	}

	public override void _Ready()
	{
		OrbitalParent = this.GetParent() as OrbitalBody2D;
	}

	public override void _Notification(int what)
	{
		if (what == NotificationParented)
		{
			var query = this.GetParent();
			if (query is Leaves leaves)
				OrbitalParent = leaves.GetOrbitalOwner();
			else
				OrbitalParent = null;
		}
		if (what == NotificationUnparented)
			OrbitalParent = null;
	}
	
	public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
		
		if (OrbitalParent != null)
		{
			var dist = OrbitalParent.GlobalPosition - this.GlobalPosition;
			var distNorm = dist.Normalized();
			Acceleration += distNorm;
		}
		
		Velocity += (Acceleration * Mass);
		Velocity += (DriveAcceleration * Mass);
		Acceleration = Acceleration.Lerp(Vector2.Zero, 0.5f);
		DriveAcceleration = DriveAcceleration.Lerp(Vector2.Zero, 0.5f);
		MoveAndSlide();
	}
}
