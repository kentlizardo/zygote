using Godot;

namespace zygote.game;

public partial class CellNode : OrbitalBody2D
{
	[Export]
	public Sprite2D CellSprite
	{
		get => _cellSprite;
		set
		{
			_cellSprite = value;
			GD.Print(_cellSprite);
			ResizeCell();
			NotifyPropertyListChanged();
		}
	}
	
	[Export] public int CellSize
	{
		get => _cellSize;
		set
		{
			_cellSize = value;
			GD.Print(_cellSize);
			ResizeCell();
			NotifyPropertyListChanged();
		}
	}
	
	private int _cellSize = 1;
	private Sprite2D _cellSprite;

	public CellNode CellParent { get; set; }

	private void ResizeCell()
	{
		this.Scale = Vector2.One * CellSize;
	}
	
	public override void _Ready()
	{
		
	}

	public override void _Process(double delta)
	{
		
	}

	public override void _Notification(int what)
	{
		if (what == NotificationUnparented)
			CellParent = null;
		if (what == NotificationParented)
		{
			var query = this.GetParent();
			if (query is Leaves leaves)
				CellParent = leaves.GetOrbitalOwner() as CellNode;
			else
				CellParent = null;
		}
	}
	
}