using System;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.Tools;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;

public class WorldInitializer : MonoBehaviour
{
    [SerializeField] private Vector2 _size;
    [SerializeField] private InitConditions _init;
    private Dictionary<Area2D.AreaType, float> _weights => _init.Weights;
    private int _nbPoints => _init.NbRandomGates;
    public ATexProvider Texture => _init.Texture;

    public InitConditions Init
    {
        get => _init;
        set => _init = value;
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
            particles[i] = new Particle(
                new Orientation(),
                new Vector2(RandomRange(lb.x, ub.x), RandomRange(lb.y, ub.y)), _size, random.Next(_init.Rules.NbSpecies)
            );
        }

        return particles;
    }

    public IEnumerable<Area2D> Points()
    {
        if (_init.RandomGates)
        {
            return RandomPoints(_size, _init.Weights);
        }
        else
        {
            Assert.IsFalse(_init.Position == null || _init.Position.Count == 0,
                "RandomGates is false, but no positions are defined in InitConditions.");
            return _init.Position.SelectMany(kvp =>
                kvp.Value.Select(v =>
                    new Area2D(
                        v * _size,
                        _init.GateSize * (_size.x + _size.y) / 2f,
                        kvp.Key)
                )
            );
        }
    }

    public IEnumerable<Area2D> RandomPoints(Vector2 worldSize, Dictionary<Area2D.AreaType, float> dictionary)
    {
        if (_nbPoints <= 0)
            yield break;
        var total = dictionary.Values.Sum();
        Assert.IsTrue(dictionary.Values.Any(v=>v>0), "One weight at least should be strictly positive > 0.");
        var radius = _init.GateSize * (worldSize.x + worldSize.y) / 2f;
        for (int i = 0; i < _nbPoints; i++)
        {
            yield return new Area2D(new Vector2(
                RandomRange(0f, worldSize.x),
                RandomRange(0f, worldSize.y)
            ), radius, WeightedRandom(dictionary, total));
        }
    }

    public T WeightedRandom<T>(Dictionary<T, float> weights, float total)
    {
        float rd = (float)random.NextDouble() * total;
        float sum = 0f;
        foreach (var weight in weights)
        {
            sum += weight.Value;
            if (rd <= sum)
                return weight.Key;
        }

        Assert.IsTrue(false);
        return default(T);
    }
}