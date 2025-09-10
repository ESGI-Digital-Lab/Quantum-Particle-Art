using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using UnityEngine;
using UnityEngine.Serialization;
using Color = UnityEngine.Color;
using Object = System.Object;
using Vector2 = Godot.Vector2;

public partial class ParticleView : Node2D, IView<Particle, ParticleWorld>
{
	[Export] private bool _showOnlyChilds = false;
	[Export] private bool _drawLines = false;
	[Export] private bool _ignoreWorldAspect = true;
	[Export] private Node2D _scale;
	[Export] private Sprite2D _sprite;
	[Export] private Line2D _line;
	private Node2D _parent;
	private Particle particle;
	private static Dictionary<Orientation, Particle> mapBack;
	private Tuple<ParticleView, ParticleView> _childs;

	public void InitView(Particle info, ParticleWorld world, Color color)
	{
		this.particle = info;
		mapBack ??= new();
		mapBack.TryAdd(info.Orientation, particle);
		_sprite.Modulate = color;
		if (_parent == null)
			_parent = this.GetParent() as Node2D;
		if (!_drawLines)
			ClearLine();
	}


	public void UpdateView(Particle info)
	{
		if (this.particle.IsSuperposed)
		{
			this.ToggleView(!_showOnlyChilds);
			if (_childs == null)
			{
				var c1 = this.Duplicate() as ParticleView;
				c1._parent = this._parent;
				this._parent.AddChild(c1);
				c1.InitView(info.Superposition.Item1, null, _sprite.Modulate);
				c1.Name = $"Super1 of {this.Name}";
				var c2 = this.Duplicate() as ParticleView;
				c2._parent = this._parent;
				this._parent.AddChild(c2);
				c2.InitView(info.Superposition.Item2, null, _sprite.Modulate);
				c2.Name = $"Super2 of {this.Name}";
				_childs = new(c1, c2);
			}

			_childs.Item1.UpdateView(info.Superposition.Item1);
			_childs.Item2.UpdateView(info.Superposition.Item2);
			if (_showOnlyChilds)
				LineTo(info.Superposition.Item1, info.Superposition.Item2.Orientation, ViewHelpers.SUP);
			else
			{
				this.UpdateView(info.Orientation);
				_childs.Item1.LineTo(info.Orientation, ViewHelpers.SUP);
				_childs.Item2.LineTo(info.Orientation, ViewHelpers.SUP);
			}
		}
		else
		{
			if (_childs != null)
			{
				_childs.Item1.ToggleView(false);
				_childs.Item2.ToggleView(false);
			}

			UpdateView(info.Orientation);
		}
	}

	public void Cleanup()
	{
		mapBack = null;
		if (_childs != null)
		{
			_childs.Item1.Cleanup();
			_childs.Item2.Cleanup();
			_childs = null;
		}

		this.QueueFree();
	}

	private void ToggleView(bool state)
	{
		_sprite.Visible = state;
	}

	public void UpdateView(Orientation Orientation)
	{
		ToggleView(true);
		this.GlobalPosition = ViewHelpers.Pos(particle.NormalizedPosition, _parent);
		//Debug.Log($"Pos for view from {particle.Position} {particle.NormalizedPosition} to {this.GlobalPosition} in local {this.Position}");
		ApplyOrientation(Orientation);
		//if (Orientation.NormalizedSpeed <= 0.0f)
		//    _renderer.material.color = Color.gray;
		if (_ignoreWorldAspect)
			_scale.GlobalScale =
				_parent.Scale.X * Vector2.One.Lerp(new Vector2(1.8f, 0.1f), Orientation.NormalizedSpeed);
		else
			_scale.Scale = Vector2.One.Lerp(new Vector2(1.8f, 0.1f), Orientation.NormalizedSpeed);
		if (Orientation.IsEntangled)
			LineTo(Orientation.Entanglement, ViewHelpers.ENT);
		else if (Orientation.IsTeleported)
			LineTo(Orientation.Teleportation, ViewHelpers.TEL);
		else
		{
			ClearLine();
		}
	}

	private void ClearLine()
	{
		_line.Points = [];
		_line.DefaultColor = Color.clear;
	}

	private void LineTo(Orientation to, Color color)
	{
		LineTo(this.particle, to, color);
	}

	private void LineTo(Particle from, Orientation to, Color color)
	{
		if (!_drawLines)
			return;
		if (mapBack.TryGetValue(to, out var target))
		{
			_line.Points =
			[
				this.ToLocal(ViewHelpers.Pos(from.NormalizedPosition, _parent)),
				this.ToLocal(ViewHelpers.Pos(target.NormalizedPosition, _parent))
			];
			_line.DefaultColor = color;
		}
	}

	private void ApplyOrientation(Orientation or)
	{
		var deg = or.Degrees;
		//TODO
	}
}
