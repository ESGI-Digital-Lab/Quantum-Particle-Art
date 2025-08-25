using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DefaultNamespace.Particle.Steps;
using Godot;

namespace UnityEngine;

public partial class GodotEntry : Node
{
    private List<MonoBehaviour> _monos;
    private Task[] _tasks;

    public override void _Ready()
    {
        _monos = new();
        List<ParticleStep> psteps = new();
        var tick = new GlobalTick();
        psteps.Add(tick);
        var Influence = new SpeciesInfluence();
        psteps.Add(Influence);
        var gates = new PointsIntersection();
        psteps.Add(gates);
        var view = new View();
        psteps.Add(view);
        List<IInit<ParticleWorld>> prewarm = new();
        var looper = new MultipleImagesLooper(InitConditionsArray(), psteps, psteps, prewarm);

        var world = new WorldInitializer(new Vector2(600, 600), 10, Vector2.zero, Vector2.one);
        looper.BaseInitializer = world;
        Add(looper);
        _tasks = _monos.Select(m => m.Awake()).ToArray();
        Task.WaitAll(_tasks);
        Debug.LogWarning("Entry mid initializing");
        _tasks = _monos.Select(m => m.Start()).ToArray();
        Task.WaitAll(_tasks);
        Debug.LogWarning("Entry finished initializing");
    }

    private static InitConditions[] InitConditionsArray()
    {
        var gatesWeights = new DictionaryFromList<Area2D.AreaType, float>(
            new()
            {
                { Area2D.AreaType.Entangle, 0.25f },
                { Area2D.AreaType.Measure, 0.25f },
                { Area2D.AreaType.Superpose, 0.25f },
                { Area2D.AreaType.Teleport, 0.25f }
            });
        InitConditions[] initConditionsArray = [
            new InitConditions(null, new RulesSaved(), new ColorPicker(), new Gates(.05f, new RandomGates(10, gatesWeights)))
        ];
        return initConditionsArray;
    }

    public override void _Process(double delta)
    {
        Time.time += (float)delta;
        for (int i = 0; i < _tasks.Length; i++)
            _tasks[i] = _monos[i].Update();
        Task.WaitAll(_tasks);
    }


    private void Add(MonoBehaviour pipe)
    {
        pipe.SetNode(this);
        _monos.Add(pipe);
    }
}