using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
public partial class SpawnConfiguration : ASpawnConfiguration
{
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

    public override IEnumerable<UnityEngine.Vector2> Particles(System.Random random)
    {
        this._random = random;
        var lb = _posMin;
        var ub = _posMax;
        if (_linSpaceOverRandom)
        {
            foreach (var vector2 in LinearReparition(lb, ub, _nbParticles)) yield return vector2;
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

    protected override UnityEngine.Vector2 BaseVelocity() => _baseVelocity;
}