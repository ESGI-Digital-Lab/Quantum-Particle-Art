using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DefaultNamespace.Tools;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Assertions;

public struct Area2D : IEquatable<Area2D>
{
    public enum AreaType
    {
        None = 0,
        Superpose = 1,
        Control = 2,
        Teleport = 3,
        Measure = 4,
    }

    private readonly Vector2 _position;
    private readonly float _radius;
    private readonly AreaType _type;

    public Area2D(Vector2 position, float radius, AreaType type)
    {
        this._position = position;
        this._radius = radius;
        this._type = type;
    }

    public AreaType Type => _type;

    public float Radius => _radius;

    public Vector2 Center => _position;

    public bool Contains(Vector2 point)
    {
        return Vector2.Distance(_position, point) <= _radius;
    }

    public bool Contains(Particle particle)
    {
        return Contains(particle.Position);
    }

    public bool Contains(Area2D other)
    {
        return Contains(other._position);
    }

    public bool Equals(Area2D other)
    {
        return _position.Equals(other._position) && _radius.Equals(other._radius) && _type == other._type;
    }

    public override bool Equals(object obj)
    {
        return obj is Area2D other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(_position, _radius, (int)_type);
    }
}

[Serializable]
public struct Ruleset
{
    [SerializeField] private string _name;
    [SerializeField, Range(0.1f, 200)] private float _diskSize;
    [SerializeField] private Species[] _rules;
    
    private static int _maxSteps;
    public Species this[int index] => _rules[index];

    [Serializable]
    public struct Species
    {
        public Species(InteractionFactor[] interactions, int steps = 0, bool averageForces = true,
            float nonAverageForce = 0.02f,
            float friction = 0.2f)
        {
            _steps = steps;
            _averageForces = averageForces;
            _nonAverageForce = nonAverageForce;
            _friction = friction;
            _interactions = interactions;
        }

        [SerializeField] private int _steps;
        [SerializeField] private bool _averageForces;
        [SerializeField] private float _nonAverageForce;
        [SerializeField] private float _friction;
        [SerializeField] private InteractionFactor[] _interactions;

        [Serializable]
        public struct InteractionFactor
        {
            public InteractionFactor(float collisionForce = 0, float collisionRadius = 0, float socialForce = 0,
                float socialRadius = 0, bool socialRamp = false)
            {
                _collisionForce = collisionForce;
                _collisionRadius = collisionRadius;
                _socialForce = socialForce;
                _socialRadius = socialRadius;
                _socialRamp = socialRamp;
            }

            [SerializeField] private float _collisionForce;
            [SerializeField] private float _collisionRadius;
            [SerializeField] private float _socialForce;
            [SerializeField] private float _socialRadius;
            [SerializeField] private bool _socialRamp;

            public float SocialRadius => _socialRadius;

            public float SocialForce
            {
                get { return _socialForce; }
                set { _socialForce = value; }
            }

            public float CollisionRadius => _collisionRadius;

            public float CollisionForce => _collisionForce;

            public bool SocialRamp => _socialRamp;
            public bool Ramp => _socialRamp;
        }

        public int Length => _interactions.Length;

        public int Steps => _steps;

        public bool AverageForces => _averageForces;

        public float NonAverageForce => _nonAverageForce;

        public float Friction => _friction;

        public InteractionFactor this[int index] => _interactions[index];
    }

    public Ruleset(Species[] rules, string name,float diskSize = 40)
    {
        _rules = rules;
        _diskSize = diskSize;
        _name = name;
    }

    [Serializable]
    public struct ForceField
    {
        [SerializeField] private float _r1;
    }

    public int NbSpecies
    {
        get
        {
            var l = _rules.Length;
            Assert.IsTrue(_rules.All(r => r.Length == l), "Rules matrix must be square");
            return l;
        }
    }

    public float DiskSize => _diskSize;
    public string Name => _name;

    public const int MaxSteps = 6;

    //[Header("Editor")] [SerializeField] private int _nbSpeciesToGenerate;
//
    //[Button("Generates empty species")]
    //public void GeneratesEmptySpecies()
    //{
    //    var rule = new Line<ForceField>
    //    {
    //        line = new ForceField[_nbSpeciesToGenerate]
    //    };
    //    _rules = Enumerable.Repeat(rule, _nbSpeciesToGenerate).ToArray();
    //}
}

public class ParticleWorld
{
    private Vector2 _size;
    private Ruleset _ruleset;
    public Ruleset Ruleset => _ruleset;

    public Vector2 Bounds => _size;
    public Vector2 Size => Bounds;
    private Particle[] _particles;
    public IEnumerable<Particle> Particles => _particles;
    public int Count => _particles.Length;
    public Particle this[int index] => _particles[index];
    private Area2D[] _pointsOfInterest;

    public IEnumerable<Area2D> PointsOfInterest => _pointsOfInterest;

    public ParticleWorld(IEnumerable<Particle> particles, IEnumerable<Area2D> points, Vector2 size, Ruleset ruleset)
    {
        _particles = particles.ToArray();
        _pointsOfInterest = points.ToArray();
        _size = size;
        _ruleset = ruleset;
        //_drawer.AddLine(new Vector2(0,0), new Vector2(1,1), Color.red);
        //_drawer.AddLine(new Vector2(.5f,0), new Vector2(.5f,1), Color.blue);
        //_drawer.AddLine(new Vector2(0,1), new Vector2(1,0), Color.red);
        //_drawer.AddLine(new Vector2(0,.5f), new Vector2(1,.5f), Color.blue);
    }

    public float WrappedDistance(Particle a, Particle b, out Vector2 normalizedDirection)
    {
        float min = float.MaxValue;
        Vector2 minDir = Vector2.zero;
        normalizedDirection = Vector2.zero;
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                Vector2 offset = new Vector2(i * _size.x, j * _size.y);
                var aPosition = a.Position;
                var bPosition = b.Position + offset;
                var dir = bPosition - aPosition;
                float dist = dir.sqrMagnitude;
                if (dist < min)
                {
                    min = dist;
                    minDir = dir;
                }
            }
        }

        min = Mathf.Sqrt(min);
        normalizedDirection = minDir / min;
        return min;
    }
}