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
	[SerializeField, Header("Settings")] private bool _showOnlyChilds = false;
	[Export] private Node2D _scale;
	[Export] private Sprite2D _sprite;
	private Particle particle;
	private static Dictionary<Orientation, Particle> mapBack;
	private Tuple<ParticleView, ParticleView> _childs;
	ParticleWorld _world;

	public void InitView(Particle info, ParticleWorld world, Color color)
	{
		this.particle = info;
		_world = world;
		mapBack ??= new();
		mapBack.TryAdd(info.Orientation, particle);
		if (world != null)
		{
			
		}
	}


	public void UpdateView(Particle info)
	{
		if (this.particle.IsSuperposed)
		{
			this.ToggleView(!_showOnlyChilds);
			if (_childs == null)
			{
				//var c1 = Instantiate(this, transform.parent);
				//c1.InitView(info.Superposition.Item1, null, _renderer.material.color);
				//c1.gameObject.name = $"Super1 of {gameObject.name} 1";
				//var c2 = Instantiate(this, transform.parent);
				//c2.InitView(info.Superposition.Item2, null, _renderer.material.color);
				//c2.gameObject.name = $"Super2 of {gameObject.name} 1";
				//_childs = new(c1, c2);
			}

			_childs.Item1.UpdateView(info.Superposition.Item1);
			_childs.Item2.UpdateView(info.Superposition.Item2);
			if (!_showOnlyChilds)
				this.UpdateView(info.Orientation);
			if (_showOnlyChilds)
				LineTo(info.Superposition.Item1, info.Superposition.Item2.Orientation, ViewHelpers.SUP);
			else
			{
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

	public void Dispose()
	{
		mapBack = null;
		if (_childs != null)
		{
			_childs.Item1.Dispose();
			_childs.Item2.Dispose();
			_childs = null;
		}

		//GameObject.Destroy(this.gameObject);
	}

	private void ToggleView(bool state)
	{
		_sprite.Visible = state;
	}

	public void UpdateView(Orientation Orientation)
	{
		ToggleView(true);
		this.Position = particle.Position;
		ApplyOrientation(Orientation);
		//if (Orientation.NormalizedSpeed <= 0.0f)
		//    _renderer.material.color = Color.gray;
		_scale.Scale = Vector2.One.Lerp(new Vector2(1.8f, 0.1f), Orientation.NormalizedSpeed);
		if (Orientation.IsEntangled)
			LineTo(Orientation.Entanglement, ViewHelpers.ENT);
		else if (Orientation.IsTeleported)
			LineTo(Orientation.Teleportation, ViewHelpers.TEL);
		//else
		//    _line.SetPositions(Array.Empty<Vector3>());
	}

	private void LineTo(Orientation to, Color color)
	{
		LineTo(this.particle, to, color);
	}

	private void LineTo(Particle from, Orientation to, Color color)
	{
		if (mapBack.TryGetValue(to, out var target))
		{
		   
		}
	}

	private void ApplyOrientation(Orientation or)
	{
		var deg = or.Degrees;
	}
}
