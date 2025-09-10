using Godot;
using UnityEngine.Assertions;
using Color = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;

public partial class PointView : Node2D, IView<Area2D, ParticleWorld>
{
	[Export] private bool _showSprite = true;
	[Export] private bool _overrideColor = false;
	[Export] private bool _showText = true;
	[Export] private Sprite2D[] _sprites;
	[Export] private RichTextLabel _label;
	private Vector2 bounds;

	public void InitView(Area2D info, ParticleWorld w, Color color)
	{
		bounds = w.Size;
		foreach (var r in _sprites)
		{
			if(_overrideColor)
				r.Modulate = color;
			r.Visible = _showSprite;
		}
		this.GlobalPosition = ViewHelpers.Pos(info.Center / bounds, this.GetParent() as Node2D);
		this.Scale = new Godot.Vector2(info.Radius / bounds.x/2f, info.Radius / bounds.y/2f);
		_label.Text = "[center]"+ToShort(info.Type)+"[/center]";
	}
	private string ToShort(Area2D.AreaType type)
	{
		return type switch
		{
			Area2D.AreaType.Entangle => "E",
			Area2D.AreaType.Measure => "M",
			Area2D.AreaType.Superpose => "H",
			Area2D.AreaType.Teleport => "T",
			_ => "?"
		};
	}
	public void UpdateView(Area2D info)
	{
		//_root.position = ViewHelpers.WorldPosition(info.Center / bounds, _root);
		//_scale.localScale = new Vector3(info.Radius / bounds.x, 0.1f, info.Radius / bounds.y);
	}

	public void Cleanup()
	{
		this.QueueFree();
	}
}
