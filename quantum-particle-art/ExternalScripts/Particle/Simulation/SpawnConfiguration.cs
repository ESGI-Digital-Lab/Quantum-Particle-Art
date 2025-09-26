using System;
using System.Collections.Generic;
using Godot;
using UnityEngine.ExternalScripts.Particle.Simulation;

[GlobalClass]
public partial class SpawnConfiguration : Resource
{
    [Export] private bool _skip = false;
    [ExportGroup("Particles")] [Export] private int _nbParticles = 1;
    [Export] private int _specyIndex = -1;
    [ExportGroup("Speed")] [Export] private float _velocityScale = 1f;
    [Export(PropertyHint.Link)] private Vector2 _baseVelocity;
    [ExportGroup("Positions")] [Export] private Vector2 _center;
    [Export(PropertyHint.Link)] private Vector2 _size;

    private Vector2 _posMin => _center - (_size / 2f);
    private Vector2 _posMax => _center + (_size / 2f);
    [Export] private bool _linSpaceOverRandom = false;
    private Random _random;

    private float RandomRange(Vector2 range)
    {
        return RandomRange(range.X, range.Y);
    }

    private float RandomRange(float min, float max)
    {
        return ((float)_random.NextDouble() * (max - min)) + min;
    }

    public IEnumerable<UnityEngine.Vector2> Particles(System.Random random)
    {
        if (_skip)
            yield break;
        this._random = random;
        var lb = _posMin;
        var ub = _posMax;
        if (_linSpaceOverRandom)
        {
            float prop = _nbParticles > 0 ? 1f / (_nbParticles) : 1f;
            for (int i = 0; i < _nbParticles; i++)
            {
                float t = (i + .5f) * (prop);
                Vector2 normalizedPos = new Vector2(Mathf.Lerp(lb.X, ub.X, t), Mathf.Lerp(lb.Y, ub.Y, t));
                yield return normalizedPos;
            }
        }
        else
        {
            for (int i = 0; i < _nbParticles; i++)
            {
                Vector2 normalizedPos = new Vector2(RandomRange(lb.X, ub.X), RandomRange(lb.Y, ub.Y));
                yield return normalizedPos;
            }
        }
    }

    public int GetSpecy(Vector2 pos, ISpecyPicker backup)
    {
        return _specyIndex >= 0 ? _specyIndex : backup.SpeciyIndex(pos);
    }

    public UnityEngine.Vector2 Velocity() => _baseVelocity * _velocityScale;
}