using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class Particle
{
    [SerializeField] private int _species = -1;
    private readonly int _totalSpecies = -1;
    [SerializeField] private Orientation _orientation;
    private Tuple<Particle, Particle> _superposition = null;
    [SerializeField] private Vector2 _position;
    [SerializeField] private Vector2 _bounds;
    [SerializeField] private Vector2[] _forces;
    private bool _wrappedLastTick = false;
    public bool WrappedLastTick => _wrappedLastTick;
    private bool _alive;


    public void SetForce(int step, Vector2 force)
    {
        _forces[step] = force;
    }


    public Particle(Vector2 normalizedPos, Vector2 bounds, int species, int totalSpecies, Vector2 baseVelocity = default)
    {
        _orientation = new Orientation(this);
        _bounds = bounds;
        _position = normalizedPos * bounds;
        _species = species;
        _forces = new Vector2[Ruleset.MaxSteps];
        _orientation.AddForce(baseVelocity);
        _alive = true;
        _totalSpecies = totalSpecies;
    }

    public Particle(Particle particle)
    {
        _orientation = new Orientation(particle._orientation, this);
        _position = particle._position;
        _species = particle._species;
        _bounds = particle._bounds;
        _forces = new Vector2[particle._forces.Length];
        _alive = particle._alive;
    }

    public IEnumerable<(Particle p, int depth)> Pivots(bool mainParticleOnly = false, bool mainParticleAlso = false,
        int depth = 0)
    {
        if (!mainParticleOnly && IsSuperposed)
        {
            foreach (var sub in _superposition.Item1.Pivots(false, mainParticleAlso, depth + 1))
                yield return sub;
            foreach (var sub in _superposition.Item2.Pivots(false, mainParticleAlso, depth + 1))
                yield return sub;
        }

        if (mainParticleAlso || !IsSuperposed)
            yield return (this, depth);
    }

    private float speed => _orientation.Speed;

    public bool IsSuperposed => _superposition != null;

    public virtual void Superpose()
    {
        if (_superposition == null)
        {
            float half = (float)Math.PI / 4f; //_orientation.Radians / 2f;
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

        _superposition = null;
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

    public IEnumerable<(Particle particle, Vector2 fromNormalized, int depth)> Tick(float deltaTime, float friction)
    {
        if (!this._alive || this.speed <= 0.0001f)
            yield break;
        foreach (var pivot in Pivots(false, true, 0))
        {
            var before = pivot.p.NormalizedPosition;
            Tick(pivot.p, deltaTime, friction, _bounds);
            //If it wrapped we just return the final position, ignore the wrapping for now
            yield return (pivot.p, pivot.p._wrappedLastTick ? pivot.p.NormalizedPosition : before, pivot.depth);
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
            if (!pivot.p.Orientation.ExternalInfluence())
                pivot.p.Orientation.AddForce(this._forces[0]);
        }
    }


    private static void Tick(Particle particle, float deltaTime, float friction, Vector2 bounds)
    {
        particle.Orientation.Friction(friction);
        particle._position += particle.Orientation.Velocity * deltaTime;
        particle._wrappedLastTick = false;
        if (particle._position.x < 0 || particle._position.x >= bounds.x)
        {
            particle._position.x = Mathf.Repeat(particle._position.x, bounds.x);
            particle._wrappedLastTick = true;
        }

        if (particle._position.y < 0 || particle._position.y >= bounds.y)
        {
            particle._position.y = Mathf.Repeat(particle._position.y, bounds.y);
            particle._wrappedLastTick = true;
        }
    }

    public void AddForce(Vector2 force)
    {
        _orientation.AddForce(force);
    }

    public void CopySpecy(Orientation teleportedFrom)
    {
        this._species = teleportedFrom.Owner.Species;
    }

    public void Warp(Vector2 position)
    {
        //So main pivot is snapped and the others are offseted relatively
        var delta = position - _position;
        foreach (var piv in Pivots(true, true))
        {
            piv.p._position += delta;
        }
    }

    public void MarkDead()
    {
        _alive = false;
    }

    public void FlipX()
    {
        _orientation.AddForce(new(-_orientation.Velocity.x * 2, 0));
    }

    public void FlipY()
    {
        _orientation.AddForce(new(0, -_orientation.Velocity.y * 2));
    }

    public void UpdateSpecies(float relative01Specy)
    {
        _species = Mathf.FloorToInt(_totalSpecies * relative01Specy)%_totalSpecies;
    }
}