using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp;
using Godot;
using UnityEngine;
using Vector2 = Godot.Vector2;

public class GeneticLooper : PipelineLooper<WorldInitializer, ParticleWorld, ParticleSimulation>
{
    private InitConditions _init;
    private Genetics _genetics;

    private IInit<WorldInitializer>[] _inits;
    private IStep<ParticleWorld>[] _steps;
    private IInit<ParticleWorld>[] _prewarm;
    private int _texHeight;
    private EncodedConfiguration _spawn => _init.Spawn;

    private static readonly object _lock = new object();

    private int _id;

    public void ExternalRestart()
    {
        _shouldRestart = true;
    }

    public GeneticLooper(int id, float duration, InitConditions init,
        EncodedConfiguration spawn,
        IEnumerable<IInit<WorldInitializer>> inits,
        IEnumerable<IStep<ParticleWorld>> step,
        IEnumerable<IInit<ParticleWorld>> prewarm,
        int texHeight)
    {
        _id = id;
        _duration = duration;
        _init = init;
        _inits = inits.ToArray();
        _steps = step.ToArray();
        _prewarm = prewarm.ToArray();
        _texHeight = texHeight;
        _genetics = new Genetics(_spawn.NbParticles, new Vector2I(_spawn.NbParticles - 2, _spawn.NbParticles));
        _genetics.OnGenerationReady += p => { _population = p.CurrentGeneration.Chromosomes; };
        _population = _genetics.GA.Population.CurrentGeneration.Chromosomes;
    }

    private IList<IChromosome> _population = null;
    private int _elapsedLoops = 0;
    private IChromosome _current = null;
    private bool _generationFinished = false;

    protected override async Task UpdateInitializer(WorldInitializer init, int loop)
    {
        loop = loop - _elapsedLoops;
        if (loop == 0)
        {
            init.Init = _init;
            await init.Init.Texture.Create();
            init.Init.Texture.Texture.Resize((int)(_texHeight * init.Init.Ratio), _texHeight,
                Image.Interpolation.Trilinear);
        }

        _current = _population[loop];
        _spawn.UpdateEncoded(_genetics.GetInput());
        _spawn.UpdateDynamicGates(_genetics.GetGates(_current));

        if (loop == _population.Count - 1)
        {
            _elapsedLoops += _population.Count;
            _generationFinished = true;
            Log("Generation finishing after this run");
        }

        //Debug.Log("Abount to wait on " + _id);
        //await Task.Delay(2000 * (_id + 1));
        //Debug.Log("Finished on " + _id);
    }

    protected override void OnFinished(ParticleSimulation pipeline)
    {
        var result = _spawn.Result();
        Log("Pipeline finished with encoder result : " + result);
        _genetics.SetResult(_current, result);
        if (_generationFinished)
        {
            Log("Generation finished, restarting GA");
            _generationFinished = false;
            _genetics.GA.Start();
            this._shouldRestart = true;
        }
    }

    protected override IEnumerable<IInit<ParticleWorld>> GetPrewarms() => _prewarm;

    protected override IEnumerable<IInit<WorldInitializer>> GetInits() => _inits;


    protected override IEnumerable<IStep<ParticleWorld>> GetSteps() => _steps;


    protected override ParticleSimulation GetPipeline()
    {
        return new ParticleSimulation();
    }

    private void Log(string value, bool withId = true)
    {
        if (withId)
            Debug.LogWarning($"[GeneticLooper {_id}] {value}");
        else
            Debug.LogWarning(value);
    }
}