using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class Particle
{
    [SerializeField] private int _species = -1;
    [SerializeField] private Orientation _orientation;
    private Tuple<Particle, Particle> _superposition = null;
    [SerializeField] private Vector2 _position;
    [SerializeField] private Vector2 _bounds;
    [SerializeField] private Vector2[] _forces;


    public void SetForce(int step, Vector2 force)
    {
        _forces[step] = force;
    }


    public Particle(Vector2 normalizedPos, Vector2 bounds, int species)
    {
        _orientation = new Orientation(this);
        _bounds = bounds;
        _position = normalizedPos * bounds;
        _species = species;
        _forces = new Vector2[Ruleset.MaxSteps];
    }

    public Particle(Particle particle)
    {
        _orientation = new Orientation(particle._orientation,this);
        _position = particle._position;
        _species = particle._species;
        _bounds = particle._bounds;
        _forces = new Vector2[particle._forces.Length];
    }

    public IEnumerable<Particle> Pivots(bool mainParticleOnly = false, bool mainParticleAlso = false)
    {
        if (!mainParticleOnly && IsSuperposed)
        {
            foreach (var sub in _superposition.Item1.Pivots(false, mainParticleAlso))
                yield return sub;
            foreach (var sub in _superposition.Item2.Pivots(false, mainParticleAlso))
                yield return sub;
        }

        if (mainParticleAlso || !IsSuperposed)
            yield return this;
    }

    private float speed => _orientation.Speed;

    public bool IsSuperposed => _superposition != null;
    public bool IsEntangled => _orientation.IsEntangled;
    public bool IsTeleported => _orientation.IsTeleported;

    public virtual void Superpose()
    {
        if (_superposition == null)
        {
            float half = (float)Math.PI / 2f;//_orientation.Radians / 2f;
            var p1 = new Particle(this);
            p1.Orientation.Radians += -half;
            var p2 = new Particle(this);
            p2.Orientation.Radians += half;
            _superposition = new(p1, p2);
        }
    }

    public Vector2 Position => _position;
    public Orientation Orientation => _orientation;

    public Tuple<Particle, Particle> Superposition => _superposition;

    public int Species => _species;
    public Vector2 NormalizedPosition => _position / _bounds;

    public void Collapse(float weightOn1 = .5f)
    {
        if (_superposition == null)
            return;
        var rd = UnityEngine.Random.Range(0f, 1f);
        if (rd < weightOn1)
        {
            CopyAndErase(_superposition.Item1);
        }
        else
        {
            CopyAndErase(_superposition.Item2);
        }
    }

    public void CopyAndErase(Particle other)
    {
        if (other._superposition != null)
            other.Collapse();
        this._orientation = other._orientation;
        this._position = other._position;
        this._species = other._species;
        this._forces = other._forces;
    }

    public IEnumerable<(Particle particle, Vector2 fromNormalized)> Tick(float deltaTime, float friction)
    {
        foreach (var pivot in Pivots(false, true))
        {
            var before = pivot.NormalizedPosition;
            Tick(pivot, deltaTime, friction, _bounds, out bool x, out bool y);
            //If it wrapped we just return the final position, ignore the wrapping for now
            yield return (pivot, x || y ? pivot.NormalizedPosition : before);
        }
    }

    public void AdvanceSteps(int maxSteps)
    {
        //If there is a step just added at 0 => maxSteps = 0 => we skip this and it will applied right away and overriden next add
        for (int i = 0; i < maxSteps; i++)
            this._forces[i] = this._forces[i + 1];
        //This particle and all recursive superpositions get the same force applied
        foreach (var pivot in Pivots(false, true))
        {
            if (!pivot.Orientation.ExternalInfluence())
                pivot.Orientation.AddForce(this._forces[0]);
        }
    }


    private static void Tick(Particle particle, float deltaTime, float friction, Vector2 bounds, out bool wrappedX,
        out bool wrappedY)
    {
        particle.Orientation.Friction(friction);
        particle._position += particle.Orientation.Velocity * deltaTime;
        wrappedX = particle._position.x < 0 || particle._position.x >= bounds.x;
        if (wrappedX)
            particle._position.x = Mathf.Repeat(particle._position.x, bounds.x);
        wrappedY = particle._position.y < 0 || particle._position.y >= bounds.y;
        
        if (wrappedY)
            particle._position.y = Mathf.Repeat(particle._position.y, bounds.y);
    }

    public void AddForce(Vector2 force)
    {
        _orientation.AddForce(force);
    }
}