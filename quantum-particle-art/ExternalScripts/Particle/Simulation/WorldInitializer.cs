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
    private ASpawnConfiguration[] _spawns;

    public WorldInitializer(float height, params ASpawnConfiguration[] spawns)
    {
        _baseheight = height;
        this._spawns = spawns;
    }

    public ATexProvider Texture => _init.Texture;

    public InitConditions Init
    {
        get => _init;
        set
        {
            _init = value;
            _size = new(_init.Ratio * _baseheight, _baseheight);
        }
    }

    public Vector2 Size => _size;

    private System.Random random;

    

    public IEnumerable<Particle> RandomCollection()
    {
        random = new System.Random(DateTime.Now.Ticks.GetHashCode());
        var particles = new List<Particle>();
        random = new System.Random(DateTime.Now.Ticks.GetHashCode());
        foreach (var spawn in _spawns.Where(s => s != null && !s.Skip))
            particles.AddRange(
                spawn.Particles(random).Select<Vector2,Particle>(v => 
                    new Particle(v, _size, spawn.GetSpecy(v,_init.SpecyPicker), spawn.Velocity
            )));
        return particles;
    }

    public IEnumerable<Area2D> Points()
    {
        var gateSize = _init.GateSize * (_size.x + _size.y) / 2f;
        return _init.Position.Select(v =>
        {
            var pos = v.pos;
            //pos.x = Math.Clamp(v.pos.x, 2 * _init.GateSize, 1 - 2 * _init.GateSize);
            //pos.y = Math.Clamp(v.pos.y, 2 * _init.GateSize, 1 - 2 * _init.GateSize);
            Debug.Log($"from {v.pos} to {pos*_size}");
            return new Area2D(pos * _size, gateSize, v.type);
        });
    }
}