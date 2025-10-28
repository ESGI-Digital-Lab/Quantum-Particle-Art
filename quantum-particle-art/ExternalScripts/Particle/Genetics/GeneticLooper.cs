using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp;
using Godot;
using KGySoft.CoreLibraries;
using UnityEngine;
using UnityEngine.ExternalScripts.Particle.Genetics;
using Vector2 = Godot.Vector2;

public class GeneticLooper : PipelineLooper<WorldInitializer, ParticleWorld, ParticleSimulation>
{
    private InitConditions _init;

    private IInit<WorldInitializer>[] _inits;
    private IStep<ParticleWorld>[] _steps;
    private IInit<ParticleWorld>[] _prewarm;
    private int _texHeight;
    private EncodedConfiguration _spawn => _init.Spawn;
    private bool _busy = false;
    private int? _result = null;

    private readonly int _id;


    public int GetResult(bool free = true)
    {
        if (free)
            _busy = false;
        return _result ?? -1;
    }

    public bool ResultAvailable => _result.HasValue;
    public bool Busy => _busy;
    private readonly Vector2I _size;
    private int _nbParticles => _init.Spawn.NbParticles;
    private object _lock = new();
    public object Lock => _lock;

    public GeneticLooper(int id, Vector2I availableSize, InitConditions init,
        IEnumerable<IInit<WorldInitializer>> inits,
        IEnumerable<IStep<ParticleWorld>> step,
        IEnumerable<IInit<ParticleWorld>> prewarm,
        int texHeight)
    {
        _id = id;
        _duration = -1;
        _init = init;
        _inits = inits.ToArray();
        _steps = step.ToArray();
        _prewarm = prewarm.ToArray();
        _texHeight = texHeight;
        _result = null;
        _busy = false;
        _size = availableSize;
    }

    public void Start(IChromosome evaluationTarget, int input)
    {
        Start(BitHelpers.GetGates(evaluationTarget, _size), input);
    }
    public void Start(IEnumerable<GateConfiguration> evaluationTarget, int input)
    {
        _busy = true;
        _result = null;
        Log("IN Lock : Updating initializer ");
        _shouldRestart = true;
        _spawn.UpdateEncoded(input);
        _spawn.UpdateDynamicGates(evaluationTarget);
    }

    public override async Task Start()
    {
        bool alreadyStarted = _shouldRestart;
        await base.Start();
        _shouldRestart = alreadyStarted;
    }

    protected override async Task<bool> UpdateInitializer(WorldInitializer init, int loop)
    {
        init.Init = _init;
        if (_texHeight > 0)
        {
            if (!init.Init.Texture.Create())
                return false;
            init.Init.Texture.Texture.Resize((int)(_texHeight * init.Init.Ratio), _texHeight,
                Image.Interpolation.Trilinear);
        }
        return true;
    }

    protected override void OnFinished(ParticleSimulation pipeline)
    {
        var result = _spawn.Result();
        _result = result;
        Log("Pipeline finished with encoder result : " + result + " checking if generation finished");
    }

    protected override IEnumerable<IInit<ParticleWorld>> GetPrewarms() => _prewarm;

    protected override IEnumerable<IInit<WorldInitializer>> GetInits() => _inits;


    protected override IEnumerable<IStep<ParticleWorld>> GetSteps() => _steps;


    protected override ParticleSimulation GetPipeline()
    {
        return new ParticleSimulation();
    }

    private void Log(string value, bool withId = true, bool withIndivId = true)
    {
        return;
        //var str = "[GeneticLooper";
        //if (withId)
        //    str += $" {_id}";
        //if (withIndivId)
        //    str += $" indivudual {_currentIndex + 1}/{_totalIndex} {_finishedCount}/{_totalIndex}/{_population.Count}";
        //str += $":Gen {_genetics.NbGen}] " + value;
        //Debug.Log(str);
    }

    public override string ToString()
    {
        return $"GeneticLooper {_id} with {_steps.Length} steps";
    }
}