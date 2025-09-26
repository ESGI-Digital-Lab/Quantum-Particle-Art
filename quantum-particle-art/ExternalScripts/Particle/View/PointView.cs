using System;
using System.Collections.Generic;
using System.Diagnostics;
using Godot;
using UnityEngine.Assertions;
using Color = UnityEngine.Color;
using Vector2 = UnityEngine.Vector2;

public partial class PointView : Node2D, IView<Area2D, ParticleWorld>
{
	[Export] private bool _showSprite = true;
	[Export] private bool _overrideColor = false;
	[Export] private bool _showText = true;
	[Export] private TextureRect[] _sprites;
	[Export] private RichTextLabel _label;
	private Vector2 bounds;

	public void InitView(Area2D info, ParticleWorld w, Color color)
	{
		bounds = w.Size;
		foreach (var r in _sprites)
		{
			if (_overrideColor)
				r.Modulate = color;
			r.Visible = _showSprite;
		}

		this.GlobalPosition = ViewHelpers.Pos(info.Center / bounds, this.GetParent() as Node2D);
		this.Scale *= new Godot.Vector2(info.Radius / bounds.x / 2f, info.Radius / bounds.y / 2f);
		_label.Text = "[center]" + ToShort(info.Gate) + "[/center]";
	}

	private static Dictionary<Type, string> _gateNames =
		new()
		{
			{ typeof(Measure), "Cx" },
			{ typeof(ControlX), "M" },
			{ typeof(Teleport), "H" },
			{ typeof(Superpose), "W" },
		};

	private string ToShort(AGate type)
	{
		if(_gateNames.TryGetValue(type.GetType(), out string gateName))
			return gateName;
		UnityEngine.Debug.Log("No gate name defined for gate of type, backing up to type first 3 letters " + type?.GetType());
		string str = type.GetType().Name;
		return str.Substring(0, Math.Min(3, str.Length));
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
