using Godot;
using System;
using zygote.game;

public partial class Leaves : Node
{
	public OrbitalBody2D GetOrbitalOwner()
	{
		var query = this.GetParent();
		while (query != null && query is not OrbitalBody2D)
		{
			query = query.GetParent();
		}
		return query as OrbitalBody2D;
	}
}
