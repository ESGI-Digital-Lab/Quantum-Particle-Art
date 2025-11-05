using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Serialization;

[Serializable]
public class Orientation
{
	private Particle _owner;
	public static float MaxSpeed = 7f;
	public float Speed
	{
		get => _velocity.magnitude;
		set => _velocity = _velocity.normalized * value;
	}

	protected Orientation _teleportedFrom = null;
	public bool IsTeleported => _teleportedFrom != null;
	private bool _isTeleportationWaiting = false;
	public bool IsControlled => _controlledBy != null;
	public Orientation Teleportation => _teleportedFrom;
	public Orientation Controller => _controlledBy;
	protected Orientation _controlledBy = null;

	public float NormalizedSpeed
	{
		get => Speed / MaxSpeed;
	}

	[SerializeField] protected Vector2 _velocity;
	public Vector2 Velocity => _velocity;

	public Orientation(Particle owner)
	{
		_velocity = Vector2.zero;
		this._owner = owner;
	}

	public Orientation(Orientation other,Particle owner) : this(owner)
	{
		this._velocity = other._velocity;
	}


	public virtual float Radians
	{
		get { return Mathf.Atan2(_velocity.y, _velocity.x); }
		set
		{
			var magnitude = _velocity.magnitude;
			_velocity = new Vector2(Mathf.Sin(value), Mathf.Cos(value)) * magnitude;
		}
	}

	public float Degrees
	{
		get => Mathf.Repeat(Radians * Mathf.Rad2Deg, 360f);
		set => Radians = Mathf.Deg2Rad * value;
	}

	public Particle Owner => _owner;


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
		Assert.IsTrue(this.Owner._totalSpecies==to.Owner._totalSpecies && this.Owner.Species < this.Owner._totalSpecies && to.Owner.Species < this.Owner._totalSpecies,
			() =>
			{
				return "Teleportation can only occur between particles of the same ruleset and valid species.";
			});
		to._teleportedFrom = this;
		to._isTeleportationWaiting = true;
	}


	public void Control(Orientation controller)
	{
		_controlledBy = controller;
	}

	public bool ExternalInfluence()
	{
		if (_controlledBy != null)
			ControlledInfluence();
		else if (_teleportedFrom != null && _isTeleportationWaiting)
		{
			TeleportedInfluence();
			//this._owner.CopySpecy(_teleportedFrom);
			//_teleportedFrom = null; // Clear the reference to avoid further influence
			_isTeleportationWaiting = false;
		}
		else
		{
			return false;
		}

		return true;
	}

	private void ControlledInfluence()
	{
		_velocity = _controlledBy._velocity;
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
