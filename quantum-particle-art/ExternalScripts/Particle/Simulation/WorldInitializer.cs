using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.Tools;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.ExternalScripts.Particle.Simulation;
using Random = System.Random;

public class WorldInitializer
{
    [SerializeField] private Vector2 _size;
    private float _baseheight;
    [SerializeField] private InitConditions _init;

    public WorldInitializer(float height,int nbParticles, Vector2 startArea, Vector2 areaSize)
    {
        _baseheight = height;
        _nbParticles = nbParticles;
        _startArea = startArea;
        _areaSize = areaSize;
    }

    public ATexProvider Texture => _init.Texture;

    public InitConditions Init
    {
        get => _init;
        set
        {
            _init = value;
            _size = new(_init.Ratio * _baseheight,_baseheight);
        }
    }

    public Vector2 Size => _size;

    private System.Random random;

    [Header("Particles")] [SerializeField, Range(1, 1000)]
    private int _nbParticles = 1000;

    [SerializeField] private Vector2 _startArea;
    [SerializeField] private Vector2 _areaSize;


    private float RandomRange(Vector2 range)
    {
        return RandomRange(range.x, range.y);
    }

    private float RandomRange(float min, float max)
    {
        return ((float)random.NextDouble() * (max - min)) + min;
    }

    public Particle[] RandomCollection()
    {
        random = new System.Random(DateTime.Now.Ticks.GetHashCode());
        var particles = new Particle[_nbParticles];
        var lb = _startArea;
        var ub = _startArea + _areaSize;
        for (int i = 0; i < _nbParticles; i++)
        {
            var normalizedPos = new Vector2(RandomRange(lb.x, ub.x), RandomRange(lb.y, ub.y));
            particles[i] = new Particle(
                new Orientation(),
                normalizedPos, _size,  _init.SpecyPicker.SpeciyIndex(normalizedPos)
            );
        }

        return particles;
    }

    public IEnumerable<Area2D> Points()
    {
        var size = _init.GateSize * (_size.x + _size.y) / 2f;
        return _init.Position.Select(v => new Area2D(v.pos * _size, size, v.type));
    }
}