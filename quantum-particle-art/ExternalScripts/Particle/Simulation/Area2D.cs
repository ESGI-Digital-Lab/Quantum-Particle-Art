using System;
using System.Collections.Generic;
using KGySoft.CoreLibraries;
using UnityEngine;

public struct Area2D : IEquatable<Area2D>
{
    //public enum AreaType
    //{
    //    None = 0,
    //    Superpose = 1,
    //    Control = 2,
    //    Teleport = 3,
    //    Measure = 4,
    //}

    private readonly Vector2 _position;
    private readonly float _radius;
    private readonly AGate _gate;
    private HashSet<Particle> _inside = new();


    private void Snap(Particle particle)
    {
        particle.Warp(_position);
    }

    public Area2D(Vector2 position, float radius, AGate gate)
    {
        this._position = position;
        this._radius = radius;
        this._gate = gate;
    }

    public AGate Gate => _gate;

    public float Radius => _radius;

    public Vector2 Center => _position;

    public bool HasInside(Vector2 point)
    {
        return Vector2.Distance(_position, point) <= _radius;
    }

    public bool HasInside(Particle particle)
    {
        return HasInside(particle.Position);
    }

    public bool HasInside(Area2D other)
    {
        return HasInside(other._position);
    }

    public bool Equals(Area2D other)
    {
        return _position.Equals(other._position) && _radius.Equals(other._radius) &&
               _gate.GetType() == other._gate.GetType();
    }

    public override bool Equals(object obj)
    {
        return obj is Area2D other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_position, _radius, _gate.GetType());
    }

    public void Handle(Particle particle)
    {
        if (this.HasInside(particle))
        {
            if (_gate.Precondition(_inside))
            {
                if (_inside.Add(particle))
                {
                    var modified = _gate.Resolve(particle);
                    if(modified)
                        this.Snap(particle);
                }
                else
                {
                    //Stays
                }
            }
        }
        else
        {
            if (_inside.Contains(particle))
                _inside.Remove(particle);
        }

    }
}