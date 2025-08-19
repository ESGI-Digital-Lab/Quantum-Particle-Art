using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

[Serializable]
public class Orientation
{
	private const float MaxSpeed = 100f;
	private float speed => _velocity.magnitude;
	protected Orientation _teleportedFrom = null;
	public bool IsTeleported => _teleportedFrom != null;
	public bool IsEntangled => _entangledBy != null;
	public Orientation Teleportation => _teleportedFrom;
	public Orientation Entanglement => _entangledBy;
	protected Orientation _entangledBy = null;

	public float NormalizedSpeed
	{
		get => speed / MaxSpeed;
	}

	[SerializeField] protected Vector2 _velocity;
	public Vector2 Velocity => _velocity;

	public Orientation()
	{
		_velocity = Vector2.zero;
	}

	public Orientation(Orientation other)
	{
		this._velocity = other._velocity;
	}

	public float Speed
	{
		get => speed;
		set => _velocity = _velocity.normalized * value;
	}

	public virtual float Radians
	{
		get { return Mathf.Atan2(_velocity.y, _velocity.x); }
		set
		{
			var magnitude = _velocity.magnitude;
			_velocity = new Vector2(Mathf.Cos(value), Mathf.Sin(value)) * magnitude;
		}
	}

	public float Degrees
	{
		get => Mathf.Repeat(Radians * Mathf.Rad2Deg, 360f);
		set => Radians = Mathf.Deg2Rad * value;
	}


	/// <summary>
	/// 
	/// </summary>
	/// <param name="target"></param>
	/// <param name="smoothSoftness"> 1 means no smoothing, 0 means important smoothing</param>
	/// <returns></returns>
	public Vector2 SmoothTo(Vector2 target, float smoothSoftness = 0.1f)
	{
		return Vector2.Lerp(_velocity, target, smoothSoftness);
	}


	public void AddForce(Vector2 force)
	{
		this._velocity += force;
	}

	public virtual void Teleport(Orientation to)
	{
		to._teleportedFrom = this;
	}


	public void Entangle(Orientation entangler)
	{
		_entangledBy = entangler;
	}

	public bool ExternalInfluence()
	{
		if (_entangledBy != null)
			EntangledInfluence();
		else if (_teleportedFrom != null)
		{
			TeleportedInfluence();
			_teleportedFrom = null; // Clear the reference to avoid further influence
		}
		else
		{
			return false;
		}

		return true;
	}

	private void EntangledInfluence()
	{
		_velocity = _entangledBy._velocity;
	}

	private void TeleportedInfluence()
	{
		this._velocity = _teleportedFrom._velocity;
		_teleportedFrom._velocity = Vector2.zero;
	}

	public void Friction(float friction)
	{
		_velocity *= 1f - friction;
	}
}
