using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GeneticSharp;
using Godot;
using KGySoft.CoreLibraries;
using UnityEngine;
using Vector2 = Godot.Vector2;

public class GeneticLooper : PipelineLooper<WorldInitializer, ParticleWorld, ParticleSimulation>
{
    private InitConditions _init;

    private IInit<WorldInitializer>[] _inits;
    private IStep<ParticleWorld>[] _steps;
    private IInit<ParticleWorld>[] _prewarm;
    private int _texHeight;
    private EncodedConfiguration _spawn => _init.Spawn;
    private IList<IChromosome> _population = null;
    private IChromosome _current = null;
    private bool running => _current != null;
    private static int _finishedCount = 0;

    private static Genetics _genetics;
    private static readonly object _lock = new object();
    private static int _totalIndex = 0;
    private int _currentIndex = -1;

    private int _id;
    private Action OnGenerationFinished;

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
        _genetics = new Genetics(_spawn.NbParticles, new Vector2I(_spawn.NbParticles - 2, _spawn.NbParticles),
            ref OnGenerationFinished);
        _genetics.OnGenerationReady += p => { _population = p.CurrentGeneration.Chromosomes; };
        _genetics.OnGenerationReady += ResetAndRestart;
        Debug.Log("First raise for init");
        OnGenerationFinished?.Invoke(); //Trigger initialization
    }

    protected override async Task UpdateInitializer(WorldInitializer init, int loop)
    {
        if (_totalIndex == 0)
        {
            init.Init = _init;
            await init.Init.Texture.Create();
            init.Init.Texture.Texture.Resize((int)(_texHeight * init.Init.Ratio), _texHeight,
                Image.Interpolation.Trilinear);
        }

        lock (_lock)
        {
            if (_totalIndex >= _population.Count)
            {
                //It means all the required index are already dispatched
                _current = null;
                return;
            }

            _currentIndex = _totalIndex;
            _totalIndex++;
            _current = _population[_currentIndex];
            Log("IN Lock : Updating initializer ");
            _spawn.UpdateEncoded(_genetics.GetInput());
            _spawn.UpdateDynamicGates(_genetics.GetGates(_current));
        }

        //Debug.Log("Abount to wait on " + _id);
        //await Task.Delay(2000 * (_id + 1));
        //Debug.Log("Finished on " + _id);
    }

    protected override void OnFinished(ParticleSimulation pipeline)
    {
        lock (_lock)
        {
            if (running)
            {
                var result = _spawn.Result();

                _finishedCount++;
                Log("Pipeline finished with encoder result : " + result + " checking if generation finished");
                _genetics.SetResult(_current, result);
                if (_finishedCount == _population.Count)
                {
                    Log("Generation finished, raising event");
                    OnGenerationFinished?.Invoke();
                }
            }
        }
    }

    private void ResetAndRestart(IPopulation pop)
    {
        lock (_lock)
        {
            _finishedCount = 0;
            _totalIndex = 0;
            _currentIndex = -1;
            this._shouldRestart = true;
            Log("New generation ready, resetting and restarting");
        }

        _population = pop.CurrentGeneration.Chromosomes;
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
        var str = "[GeneticLooper";
        if (withId)
            str += $" {_id}";
        if (withIndivId)
            str += $" indivudual {_currentIndex + 1}/{_totalIndex} {_finishedCount}/{_totalIndex}/{_population.Count}";
        str += "] " + value;
        Debug.Log(str);
    }
}